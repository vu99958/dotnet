using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using QuanLyNhanSu.Domain; // Cho UserKey
using QuanLyNhanSu.Permissions;

namespace QuanLyNhanSu
{
    [Authorize]
    public class LeaveRequestAppService :
        CrudAppService<
            LeaveRequest, 
            LeaveRequestDto, 
            Guid, 
            PagedAndSortedResultRequestDto, 
            CreateUpdateLeaveRequestDto>, 
        ILeaveRequestAppService 
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public LeaveRequestAppService(
            IRepository<LeaveRequest, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<UserKey, Guid> userKeyRepository)
            : base(repository)
        {
            _userRepository = userRepository;
            _userKeyRepository = userKeyRepository;
        }

        // --- Hàm phụ trợ: Lấy role từ bảng UserKey ---
        private async Task<string> GetUserRoleAsync(Guid userId)
        {
            var key = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId);
            if (key != null)
            {
                return key.Role.ToLower();
            }

            // Fallback: Nếu không có trong UserKey, kiểm tra xem có phải tài khoản admin mặc định không
            var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && user.UserName.ToLower() == "admin")
            {
                return "superadmin"; // Admin hệ thống mặc định là superadmin
            }

            return "user";
        }

        protected override async Task<IQueryable<LeaveRequest>> CreateFilteredQueryAsync(PagedAndSortedResultRequestDto input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            var currentUserId = CurrentUser.Id ?? Guid.Empty;
            string myRole = await GetUserRoleAsync(currentUserId);

            // Nếu không phải admin hoặc superadmin, chỉ thấy đơn của chính mình
            if (myRole != "admin" && myRole != "superadmin")
            {
                query = query.Where(x => x.UserId == currentUserId);
            }

            return query;
        }

        public override async Task<PagedResultDto<LeaveRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var query = await CreateFilteredQueryAsync(input);
            var totalCount = await AsyncExecuter.CountAsync(query);
            var entities = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.StartDate).Skip(input.SkipCount).Take(input.MaxResultCount)
            );

            var dtos = new List<LeaveRequestDto>();
            foreach (var entity in entities)
            {
                var dto = MapToGetOutputDto(entity);
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == entity.UserId);
                dto.UserName = user != null ? user.UserName : "Không xác định";
                dtos.Add(dto);
            }

            return new PagedResultDto<LeaveRequestDto>(totalCount, dtos);
        }

        public override async Task<LeaveRequestDto> CreateAsync(CreateUpdateLeaveRequestDto input)
        {
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            
            if (input.StartDate.Date > input.EndDate.Date)
            {
                throw new UserFriendlyException("Từ chối: Ngày kết thúc không thể nhỏ hơn ngày bắt đầu!");
            }

            // Kiểm tra: 1 user không được có 2 đơn trùng lặp thời gian trong cùng một ngày
            // Loại trừ các đơn đã bị Từ chối (Rejected)
            bool isOverlap = await Repository.AnyAsync(x => 
                x.UserId == currentUserId && 
                x.Status != "Rejected" &&
                x.StartDate.Date <= input.EndDate.Date && 
                x.EndDate.Date >= input.StartDate.Date);
            
            if (isOverlap)
            {
                throw new UserFriendlyException("Từ chối: Bạn đã có đơn xin nghỉ phép (Đang chờ hoặc Đã duyệt) trong khoảng thời gian này!");
            }

            input.UserId = currentUserId;
            input.Status = "Pending";
            return await base.CreateAsync(input);
        }

        public override async Task<LeaveRequestDto> UpdateAsync(Guid id, CreateUpdateLeaveRequestDto input)
        {
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);
            
            var entity = await Repository.GetAsync(id);
            
            // Bảo mật: User thường không được sửa đơn của người khác
            if (myRole != "admin" && myRole != "superadmin" && entity.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("Từ chối: Bạn không thể sửa đơn của người khác.");
            }

            if (entity.Status != "Pending")
            {
                throw new UserFriendlyException("Chỉ được sửa đơn khi ở trạng thái Đang chờ.");
            }
            
            if (input.StartDate.Date > input.EndDate.Date)
            {
                throw new UserFriendlyException("Từ chối: Ngày kết thúc không thể nhỏ hơn ngày bắt đầu!");
            }
            
            // Kiểm tra trùng lịch (loại trừ đơn hiện tại và các đơn đã bị Từ chối)
            bool isOverlap = await Repository.AnyAsync(x => 
                x.Id != id &&
                x.UserId == entity.UserId && 
                x.Status != "Rejected" &&
                x.StartDate.Date <= input.EndDate.Date && 
                x.EndDate.Date >= input.StartDate.Date);
            
            if (isOverlap)
            {
                throw new UserFriendlyException("Từ chối: Khoảng thời gian sửa lại bị trùng với một đơn khác của bạn!");
            }

            input.UserId = entity.UserId;
            input.Status = entity.Status;

            return await base.UpdateAsync(id, input);
        }

        public override async Task DeleteAsync(Guid id)
        {
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);
            var entity = await Repository.GetAsync(id);

            if (myRole == "admin" || myRole == "superadmin")
            {
                // Quản lý: Không được xóa đơn nếu chưa xác nhận (đang Pending)
                if (entity.Status == "Pending")
                {
                    throw new UserFriendlyException("Quản lý không được phép xóa đơn khi chưa xác nhận. Vui lòng Duyệt hoặc Từ chối trước!");
                }
            }
            else
            {
                // User thường: Chỉ được xóa (rút đơn) khi đang Pending
                if (entity.UserId != currentUserId)
                {
                    throw new UnauthorizedAccessException("Từ chối: Bạn không thể xóa đơn của người khác.");
                }
                if (entity.Status != "Pending")
                {
                    throw new UserFriendlyException("Chỉ được rút đơn khi ở trạng thái Đang chờ.");
                }
            }

            await base.DeleteAsync(id);
        }

        public async Task ChangeStatusAsync(Guid id, string newStatus)
        {
            if (newStatus != "Approved" && newStatus != "Rejected")
            {
                throw new UserFriendlyException("Trạng thái mới không hợp lệ.");
            }

            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);

            if (myRole != "admin" && myRole != "superadmin")
            {
                throw new UnauthorizedAccessException("Bạn không có quyền duyệt đơn.");
            }

            var entity = await Repository.GetAsync(id);
            
            // Lấy role của người tạo đơn
            string targetRole = await GetUserRoleAsync(entity.UserId);

            // Kiểm tra phân quyền: admin duyệt user, superadmin duyệt admin và user
            if (targetRole == "superadmin")
            {
                throw new UserFriendlyException("Không ai có thể duyệt đơn của Super Admin (mặc định đã duyệt)!");
            }
            if (targetRole == "admin" && myRole != "superadmin")
            {
                throw new UserFriendlyException("Từ chối: Bạn không thể duyệt đơn của Admin khác. Phải là Super Admin!");
            }

            entity.Status = newStatus;
            await Repository.UpdateAsync(entity);
        }

        protected override LeaveRequest MapToEntity(CreateUpdateLeaveRequestDto createInput)
        {
            return new LeaveRequest(
                GuidGenerator.Create(),
                createInput.UserId,
                createInput.StartDate,
                createInput.EndDate,
                createInput.Reason,
                createInput.Status
            );
        }

        protected override void MapToEntity(CreateUpdateLeaveRequestDto updateInput, LeaveRequest entity)
        {
            entity.UserId = updateInput.UserId;
            entity.StartDate = updateInput.StartDate;
            entity.EndDate = updateInput.EndDate;
            entity.Reason = updateInput.Reason;
            entity.Status = updateInput.Status;
        }

        protected override LeaveRequestDto MapToGetOutputDto(LeaveRequest entity)
        {
            return new LeaveRequestDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                UserName = "", 
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Reason = entity.Reason,
                Status = entity.Status
            };
        }
    }
}
