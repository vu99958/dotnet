using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace QuanLyNhanSu
{
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
                x.Status == "Approved" &&
                x.StartDate <= endDate && x.EndDate >= startDate);
            var existingPayslips = await _payslipRepository.GetListAsync(x => x.Month == month && x.Year == year);

            foreach (var profile in salaryProfiles)
            {
                var userId = profile.UserId;

                // ──────────────────────────────────────────────
                // 1. NGÀY CÔNG THỰC TẾ
                // ──────────────────────────────────────────────
                var userAttendances = attendances
                    .Where(x => x.UserId == userId &&
                           (x.Status == "Đúng giờ" || x.Status == "Đi trễ" || x.Status == "Về sớm"))
                    .ToList();
                int actualWorkDays = userAttendances.Count;

                // ──────────────────────────────────────────────
                // 2. PHẠT ĐI TRỄ / VỀ SỚM
                // ──────────────────────────────────────────────
                int totalLateMinutes = userAttendances.Sum(x => x.LateMinutes);
                int totalEarlyMinutes = userAttendances.Sum(x => x.EarlyLeaveMinutes);
                decimal totalPenalty = (totalLateMinutes + totalEarlyMinutes) * 2000m;

                // ──────────────────────────────────────────────
                // 3. NGÀY PHÉP CÓ LƯƠNG
                // ──────────────────────────────────────────────
                var userLeaves = leaves.Where(x => x.UserId == userId).ToList();
                int approvedLeaveDays = 0;
                foreach (var leave in userLeaves)
                {
                    // Chỉ tính số ngày nghỉ NẰM TRONG tháng đang chốt
                    var leaveStart = leave.StartDate.Date > startDate ? leave.StartDate.Date : startDate;
                    var leaveEnd = leave.EndDate.Date < endDate ? leave.EndDate.Date : endDate;
                    if (leaveEnd >= leaveStart)
                    {
                        approvedLeaveDays += (int)(leaveEnd - leaveStart).TotalDays + 1;
                    }
                }

                // ──────────────────────────────────────────────
                // 4. TÍNH TĂNG CA (OVERTIME)
                // Nếu tổng ngày làm + ngày phép > ngày chuẩn → phần dư là tăng ca
                // Tăng ca được trả 150% lương ngày (theo Luật Lao động VN)
                // ──────────────────────────────────────────────
                decimal dailySalary = profile.BaseSalary / standardWorkDays;
                int totalPaidDays = actualWorkDays + approvedLeaveDays;
                int regularDays = Math.Min(totalPaidDays, standardWorkDays);
                int overtimeDays = Math.Max(0, totalPaidDays - standardWorkDays);
                decimal overtimePay = overtimeDays * dailySalary * 1.5m;

                // ──────────────────────────────────────────────
                // 5. TÍNH LƯƠNG GROSS & NET
                // Gross = Lương ngày thường + Phụ cấp + Tăng ca - Phạt
                // Net   = Gross × 89.5% (trừ 10.5% BHXH + BHYT + BHTN)
                // ──────────────────────────────────────────────
                decimal grossSalary = (dailySalary * regularDays) + profile.Allowance + overtimePay - totalPenalty;
                if (grossSalary < 0) grossSalary = 0;
                decimal netSalary = grossSalary * 0.895m;

                // ──────────────────────────────────────────────
                // 6. LƯU KẾT QUẢ (Upsert: Cập nhật nếu có, Tạo mới nếu chưa)
                // ──────────────────────────────────────────────
                var payslip = existingPayslips.FirstOrDefault(p => p.UserId == userId);
                if (payslip == null)
                {
                    payslip = new Payslip(
                        GuidGenerator.Create(),
                        userId, month, year,
                        standardWorkDays, actualWorkDays, approvedLeaveDays,
                        overtimeDays, overtimePay,
                        totalPenalty, grossSalary, netSalary
                    );
                    await _payslipRepository.InsertAsync(payslip);
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
                    await _payslipRepository.UpdateAsync(payslip);
                }
            }
        }

        public async Task<List<PayslipDto>> GetListAsync(int month, int year)
        {
            var payslips = await _payslipRepository.GetListAsync(x => x.Month == month && x.Year == year);
            
            // Nếu không phải admin, chỉ lấy phiếu lương của chính mình
            if (CurrentUser.Id.HasValue && !CurrentUser.Roles.Contains("admin"))
            {
                payslips = payslips.Where(x => x.UserId == CurrentUser.Id.Value).ToList();
            }

            var users = await _userRepository.GetListAsync();

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
