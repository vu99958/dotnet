using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain; // Đã thêm thư viện truy xuất CSDL

namespace QuanLyNhanSu
{
    [Authorize] 
    public class MyProfileAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;
        private readonly IRepository<Branch, Guid> _branchRepository;

        // Nhúng cả 3 công cụ vào hàm khởi tạo
        public MyProfileAppService(
            IdentityUserManager userManager, 
            IRepository<UserKey, Guid> userKeyRepository,
            IRepository<Branch, Guid> branchRepository)
        {
            _userManager = userManager;
            _userKeyRepository = userKeyRepository;
            _branchRepository = branchRepository;
        }

        public async Task<MyProfileDto> GetMyProfileAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null)
                throw new UnauthorizedAccessException("Lỗi bảo mật: Không tìm thấy thông tin.");

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            // 1. Lấy danh sách quyền từ hệ thống lõi Identity
            var roles = await _userManager.GetRolesAsync(user);
            string roleDisplay = string.Join(", ", roles);

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId.Value);
            if (string.IsNullOrEmpty(roleDisplay))
            {
                if (userKey != null)
                {
                    roleDisplay = userKey.Role; // Lấy đúng cái Role "admin" mà bạn đã tạo
                }
            }

            string branchName = "Chưa phân bổ";
            if (userKey?.BranchId != null)
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == userKey.BranchId.Value);
                if (branch != null) branchName = branch.Name;
            }

            return new MyProfileDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roleDisplay, // Trả về role đã được fix
                BranchName = branchName,
                CreationTime = user.CreationTime
            };
        }
    }
}   