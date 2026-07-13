using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Permissions;
using QuanLyNhanSu.Settings;

namespace QuanLyNhanSu
{
    [Authorize]
    public class PayslipAppService : QuanLyNhanSuAppService, IPayslipAppService
    {
        private readonly IRepository<SalaryProfile, Guid> _salaryProfileRepository;
        private readonly IRepository<Payslip, Guid> _payslipRepository;
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<LeaveRequest, Guid> _leaveRequestRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public PayslipAppService(
            IRepository<SalaryProfile, Guid> salaryProfileRepository,
            IRepository<Payslip, Guid> payslipRepository,
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<LeaveRequest, Guid> leaveRequestRepository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _salaryProfileRepository = salaryProfileRepository;
            _payslipRepository = payslipRepository;
            _attendanceRepository = attendanceRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Tính số ngày làm việc chuẩn của 1 tháng (loại trừ Thứ 7 và Chủ Nhật).
        /// VD: Tháng 6/2026 có 22 ngày, Tháng 2/2026 có 20 ngày.
        /// </summary>
        private int CalculateStandardWorkDays(int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int workDays = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                // Loại trừ Thứ 7 (Saturday) và Chủ Nhật (Sunday)
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workDays++;
                }
            }

            return workDays;
        }

        public async Task GenerateMonthlyPayrollAsync(int month, int year)
        {
            // Xác định khoảng thời gian tháng (tối ưu cho SQL Index)
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Tính ngày công chuẩn TỰ ĐỘNG theo lịch tháng (loại trừ T7, CN)
            int standardWorkDays = CalculateStandardWorkDays(month, year);

            // Lấy toàn bộ dữ liệu cần thiết (Batch Query — tránh N+1)
            var salaryProfiles = await _salaryProfileRepository.GetListAsync();
            var attendances = await _attendanceRepository.GetListAsync(x => x.WorkDate >= startDate && x.WorkDate <= endDate);
            var leaves = await _leaveRequestRepository.GetListAsync(x =>
                x.Status == QuanLyNhanSu.Enums.LeaveRequestStatus.Approved &&
                x.StartDate <= endDate && x.EndDate >= startDate);
            var existingPayslips = await _payslipRepository.GetListAsync(x => x.Month == month && x.Year == year);

            // ──────────────────────────────────────────────
            // FETCH SETTINGS (SaaS Ready)
            // ──────────────────────────────────────────────
            var latePenaltySetting = await SettingProvider.GetOrNullAsync("QuanLyNhanSu.Payroll.LatePenaltyPerMinute") ?? "2000";
            var netSalaryRateSetting = await SettingProvider.GetOrNullAsync("QuanLyNhanSu.Payroll.NetSalaryRate") ?? "0.895";
            
            decimal latePenaltyPerMinute = decimal.Parse(latePenaltySetting);
            decimal netSalaryRate = decimal.Parse(netSalaryRateSetting);

            // [ONBOARDING COMMENT]: Gom nhóm dữ liệu vào In-Memory Dictionary/Lookup để xử lý với độ phức tạp O(N) thay vì O(N^2) khi dùng .Where() trong vòng lặp.
            var attendanceLookup = attendances.Where(x => x.CheckInTime != null).ToLookup(x => x.UserId);
            var leaveLookup = leaves.ToLookup(x => x.UserId);
            var existingPayslipsDict = existingPayslips.ToDictionary(x => x.UserId);

            var payslipsToInsert = new List<Payslip>();
            var payslipsToUpdate = new List<Payslip>();

            foreach (var profile in salaryProfiles)
            {
                var userId = profile.UserId;

                // 1. NGÀY CÔNG THỰC TẾ
                var userAttendances = attendanceLookup[userId].ToList();
                
                double actualWorkDays = 0;
                foreach (var att in userAttendances)
                {
                    if (att.CheckInTime != null && att.CheckOutTime != null)
                    {
                        actualWorkDays += 1.0;
                    }
                    else if (att.CheckInTime != null && att.CheckOutTime == null)
                    {
                        actualWorkDays += 0.5; // Phạt nhân viên quên Check-out (chỉ tính nửa công)
                    }
                }

                // 2. PHẠT ĐI TRỄ / VỀ SỚM
                int totalLateMinutes = userAttendances.Sum(x => x.LateMinutes);
                int totalEarlyMinutes = userAttendances.Sum(x => x.EarlyLeaveMinutes);
                decimal totalPenalty = (totalLateMinutes + totalEarlyMinutes) * latePenaltyPerMinute;

                // 3. NGÀY PHÉP CÓ LƯƠNG (Loại trừ Thứ 7, Chủ Nhật)
                var userLeaves = leaveLookup[userId].ToList();
                int approvedLeaveDays = 0;
                foreach (var leave in userLeaves)
                {
                    var leaveStart = leave.StartDate.Date > startDate ? leave.StartDate.Date : startDate;
                    var leaveEnd = leave.EndDate.Date < endDate ? leave.EndDate.Date : endDate;
                    if (leaveEnd >= leaveStart)
                    {
                        for (var d = leaveStart; d <= leaveEnd; d = d.AddDays(1))
                        {
                            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                            {
                                approvedLeaveDays++;
                            }
                        }
                    }
                }

                // 4. TÍNH TĂNG CA (OVERTIME)
                decimal dailySalary = profile.BaseSalary / standardWorkDays;
                double totalPaidDays = actualWorkDays + approvedLeaveDays;
                
                if (totalPaidDays > standardWorkDays)
                {
                    totalPaidDays = standardWorkDays;
                }

                int overtimeDays = 0;
                decimal overtimePay = 0;

                // 5. TÍNH LƯƠNG GROSS & NET
                decimal regularDays = (decimal)totalPaidDays;
                decimal grossSalary = (dailySalary * regularDays) + profile.Allowance + overtimePay - totalPenalty;
                if (grossSalary < 0) grossSalary = 0;
                decimal netSalary = grossSalary * netSalaryRate;

                // 6. CHUẨN BỊ LƯU KẾT QUẢ
                if (!existingPayslipsDict.TryGetValue(userId, out var payslip))
                {
                    payslip = new Payslip(
                        GuidGenerator.Create(),
                        userId, month, year,
                        standardWorkDays, actualWorkDays, approvedLeaveDays,
                        overtimeDays, overtimePay,
                        totalPenalty, grossSalary, netSalary
                    );
                    payslipsToInsert.Add(payslip);
                }
                else
                {
                    payslip.StandardWorkDays = standardWorkDays;
                    payslip.ActualWorkDays = actualWorkDays;
                    payslip.ApprovedLeaveDays = approvedLeaveDays;
                    payslip.OvertimeDays = overtimeDays;
                    payslip.OvertimePay = overtimePay;
                    payslip.TotalPenalty = totalPenalty;
                    payslip.GrossSalary = grossSalary;
                    payslip.NetSalary = netSalary;
                    payslipsToUpdate.Add(payslip);
                }
            }

            // [ONBOARDING COMMENT]: KHÔNG truy vấn hay ghi DB trong vòng lặp. Bắt buộc dùng InsertManyAsync và UpdateManyAsync để xử lý Bulk Data, cắt giảm 10,000 DB Round-trips xuống còn 2.
            if (payslipsToInsert.Any())
            {
                await _payslipRepository.InsertManyAsync(payslipsToInsert);
            }
            if (payslipsToUpdate.Any())
            {
                await _payslipRepository.UpdateManyAsync(payslipsToUpdate);
            }
        }

        public async Task<List<PayslipDto>> GetListAsync(int month, int year)
        {
            var payslips = await _payslipRepository.GetListAsync(x => x.Month == month && x.Year == year);
            
            // Phân quyền chuẩn ABP (Anti Manual-Role-Check)
            if (CurrentUser.Id.HasValue)
            {
                var isAdmin = await AuthorizationService.IsGrantedAsync(QuanLyNhanSuPermissions.Payslip.Manage);
                if (!isAdmin)
                {
                    payslips = payslips.Where(x => x.UserId == CurrentUser.Id.Value).ToList();
                }
            }

            // [ONBOARDING COMMENT]: Lọc danh sách ID thay vì load toàn bộ bảng Users vào RAM (chống N+1 Query & Memory Bloat) theo chuẩn Enterprise.
            var userIds = payslips.Select(p => p.UserId).Distinct().ToList();
            var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));

            var query = from p in payslips
                        join u in users on p.UserId equals u.Id
                        select new PayslipDto
                        {
                            Id = p.Id,
                            UserId = p.UserId,
                            UserName = u.Name ?? u.UserName,
                            Month = p.Month,
                            Year = p.Year,
                            StandardWorkDays = p.StandardWorkDays,
                            ActualWorkDays = p.ActualWorkDays,
                            ApprovedLeaveDays = p.ApprovedLeaveDays,
                            OvertimeDays = p.OvertimeDays,
                            OvertimePay = p.OvertimePay,
                            TotalPenalty = p.TotalPenalty,
                            GrossSalary = p.GrossSalary,
                            NetSalary = p.NetSalary
                        };

            return query.ToList();
        }
    }
}
