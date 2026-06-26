using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace QuanLyNhanSu
{
    public class PayslipComplaintAppService : ApplicationService, IPayslipComplaintAppService
    {
        private readonly IRepository<PayslipComplaint, Guid> _complaintRepository;
        private readonly IIdentityUserRepository _userRepository;

        public PayslipComplaintAppService(
            IRepository<PayslipComplaint, Guid> complaintRepository,
            IIdentityUserRepository userRepository)
        {
            _complaintRepository = complaintRepository;
            _userRepository = userRepository;
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
            // Admin only
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
            if (!CurrentUser.Roles.Contains("admin"))
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền giải quyết khiếu nại.");

            var complaint = await _complaintRepository.GetAsync(id);
            complaint.Status = input.Status;
            complaint.AdminReply = input.AdminReply;
            await _complaintRepository.UpdateAsync(complaint);
        }
    }
}
