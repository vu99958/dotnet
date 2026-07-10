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

namespace QuanLyNhanSu
{
    /// <summary>
    /// Service thống kê cho Dashboard — chỉ Admin/SuperAdmin được phép gọi.
    /// </summary>
    [Authorize(QuanLyNhanSuPermissions.Dashboard.Default)]
    public class DashboardAppService : QuanLyNhanSuAppService, IDashboardAppService
    {
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<Payslip, Guid> _payslipRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public DashboardAppService(
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<Payslip, Guid> payslipRepository,
            IRepository<UserKey, Guid> userKeyRepository)
        {
            _attendanceRepository = attendanceRepository;
            _payslipRepository = payslipRepository;
            _userKeyRepository = userKeyRepository;
        }

        /// <summary>
        /// Thống kê chấm công hôm nay:
        /// - OnTimeCount  = bản ghi có LateMinutes == 0 VÀ EarlyLeaveMinutes == 0
        /// - LateOrEarlyCount = tất cả bản ghi còn lại (đi trễ hoặc về sớm)
        /// </summary>
        public async Task<TodayAttendanceStatsDto> GetTodayAttendanceStatsAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) throw new UnauthorizedAccessException("Chưa đăng nhập");

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId);
            if (userKey == null || (userKey.Role.ToLower() != "admin" && userKey.Role.ToLower() != "superadmin"))
                throw new UnauthorizedAccessException("Không có quyền truy cập");

            var today = DateTime.Now.Date;

            var todayRecords = await _attendanceRepository.GetListAsync(
                x => x.WorkDate == today);

            int onTime = todayRecords.Count(r => r.LateMinutes == 0 && r.EarlyLeaveMinutes == 0);
            int lateOrEarly = todayRecords.Count - onTime;

            return new TodayAttendanceStatsDto
            {
                OnTimeCount = onTime,
                LateOrEarlyCount = lateOrEarly
            };
        }

        /// <summary>
        /// Thống kê quỹ lương Net toàn công ty theo từng tháng trong năm hiện tại.
        /// Trả về đủ 12 tháng; tháng chưa có dữ liệu = 0.
        /// </summary>
        public async Task<List<MonthlySalaryStatsDto>> GetMonthlySalaryStatsAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) throw new UnauthorizedAccessException("Chưa đăng nhập");

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId);
            if (userKey == null || (userKey.Role.ToLower() != "admin" && userKey.Role.ToLower() != "superadmin"))
                throw new UnauthorizedAccessException("Không có quyền truy cập");

            int currentYear = DateTime.Now.Year;

            var yearPayslips = await _payslipRepository.GetListAsync(
                x => x.Year == currentYear);

            // Group by tháng, sum NetSalary
            var grouped = yearPayslips
                .GroupBy(p => p.Month)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.NetSalary));

            // Tạo đủ 12 tháng, tháng không có data = 0
            var result = new List<MonthlySalaryStatsDto>();
            for (int m = 1; m <= 12; m++)
            {
                result.Add(new MonthlySalaryStatsDto
                {
                    Month = m,
                    TotalNetSalary = grouped.ContainsKey(m) ? grouped[m] : 0m
                });
            }

            return result;
        }
    }
}
