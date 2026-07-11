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
    [Authorize]
    public class PayslipComplaintAppService : QuanLyNhanSuAppService, IPayslipComplaintAppService
    {
        private readonly IRepository<PayslipComplaint, Guid> _complaintRepository;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public PayslipComplaintAppService(
            IRepository<PayslipComplaint, Guid> complaintRepository,
            IIdentityUserRepository userRepository,
            IRepository<UserKey, Guid> userKeyRepository)
        {
            _complaintRepository = complaintRepository;
            _userRepository = userRepository;
            _userKeyRepository = userKeyRepository;
        }

        public async Task<List<PayslipComplaintDto>> GetMyComplaintsAsync()
        {
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null) return new List<PayslipComplaintDto>();

            var complaints = await _complaintRepository.GetListAsync(c => c.UserId == currentUserId.Value);
            var users = await _userRepository.GetListAsync();

            return (from c in complaints
                    join u in users on c.UserId equals u.Id
                    orderby c.CreationTime descending
                    select new PayslipComplaintDto
                    {
                        Id = c.Id,
                        PayslipId = c.PayslipId,
                        UserId = c.UserId,
                        UserName = u.Name ?? u.UserName,
                        Month = c.Month,
                        Year = c.Year,
                        Reason = c.Reason,
                        Status = c.Status,
                        AdminReply = c.AdminReply,
                        CreationTime = c.CreationTime
                    }).ToList();
        }

        public async Task<List<PayslipComplaintDto>> GetPendingListAsync()
        {
            // BUG-11 FIX: Kiểm tra quyền Admin trước khi trả dữ liệu
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null) throw new UnauthorizedAccessException("Chưa đăng nhập");

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId.Value);
            bool isAdmin = userKey != null && (userKey.Role.ToLower() == "admin" || userKey.Role.ToLower() == "superadmin");
            if (!isAdmin)
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền xem danh sách khiếu nại.");

            var complaints = await _complaintRepository.GetListAsync(c => c.Status == "Pending");
            var users = await _userRepository.GetListAsync();

            return (from c in complaints
                    join u in users on c.UserId equals u.Id
                    orderby c.CreationTime ascending
                    select new PayslipComplaintDto
                    {
                        Id = c.Id,
                        PayslipId = c.PayslipId,
                        UserId = c.UserId,
                        UserName = u.Name ?? u.UserName,
                        Month = c.Month,
                        Year = c.Year,
                        Reason = c.Reason,
                        Status = c.Status,
                        AdminReply = c.AdminReply,
                        CreationTime = c.CreationTime
                    }).ToList();
        }

        public async Task CreateAsync(CreateComplaintDto input)
        {
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null) throw new UnauthorizedAccessException("Not logged in");

            var entity = new PayslipComplaint(
                GuidGenerator.Create(),
                input.PayslipId,
                currentUserId.Value,
                input.Month,
                input.Year,
                input.Reason
            );

            await _complaintRepository.InsertAsync(entity);
        }

        public async Task ResolveAsync(Guid id, ResolveComplaintDto input)
        {
            // BUG-03 FIX: Dùng UserKey.Role thay vì CurrentUser.Roles
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null) throw new UnauthorizedAccessException("Chưa đăng nhập");

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId.Value);
            bool isAdmin = userKey != null && (userKey.Role.ToLower() == "admin" || userKey.Role.ToLower() == "superadmin");
            if (!isAdmin)
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền giải quyết khiếu nại.");

            var complaint = await _complaintRepository.GetAsync(id);
            complaint.Status = input.Status;
            complaint.AdminReply = input.AdminReply;
            await _complaintRepository.UpdateAsync(complaint);
        }
    }
}
