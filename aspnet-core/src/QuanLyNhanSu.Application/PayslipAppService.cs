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
using Volo.Abp.BackgroundJobs;
using QuanLyNhanSu.Payroll.BackgroundJobs;

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
        private readonly IBackgroundJobManager _backgroundJobManager;

        public PayslipAppService(
            IRepository<SalaryProfile, Guid> salaryProfileRepository,
            IRepository<Payslip, Guid> payslipRepository,
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<LeaveRequest, Guid> leaveRequestRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IBackgroundJobManager backgroundJobManager)
        {
            _salaryProfileRepository = salaryProfileRepository;
            _payslipRepository = payslipRepository;
            _attendanceRepository = attendanceRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _userRepository = userRepository;
            _backgroundJobManager = backgroundJobManager;
        }

        // [ONBOARDING COMMENT]: Thay vì chạy logic đồng bộ làm nghẽn Server, ta chỉ làm 1 việc: Bắn Job vào Hangfire Queue và trả về thành công ngay lập tức (Mất chưa tới 5ms).
        public async Task<string> GenerateMonthlyPayrollAsync(int month, int year)
        {
            await _backgroundJobManager.EnqueueAsync(new PayrollCalculationJobArgs
            {
                TenantId = CurrentTenant.Id,
                Month = month,
                Year = year
            });

            return $"Tác vụ tính lương tháng {month}/{year} đã được đưa vào hàng đợi chạy ngầm thành công. Vui lòng kiểm tra màn hình Background Jobs.";
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
