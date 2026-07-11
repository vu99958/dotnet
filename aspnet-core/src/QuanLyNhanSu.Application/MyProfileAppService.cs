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
            
            // BUG FIX: LUÔN LUÔN ưu tiên quyền từ UserKey (nếu có) vì hệ thống 
            // hiện tại phân quyền (admin/user) dựa trên bảng UserKey. 
            // Nếu không ghi đè, Identity role mặc định (vd: "user") sẽ làm ẩn giao diện Admin.
            if (userKey != null && !string.IsNullOrEmpty(userKey.Role))
            {
                roleDisplay = userKey.Role; 
            }
            else if (string.IsNullOrEmpty(roleDisplay))
            {
                roleDisplay = "user"; // Fallback
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

        /// <summary>
        /// Cập nhật thông tin cá nhân (Email, Phone).
        /// BUG-08 FIX: Thêm API để nút "Lưu" trên WinForms hoạt động thật.
        /// </summary>
        public async Task UpdateMyProfileAsync(UpdateMyProfileDto input)
        {
            var userId = CurrentUser.Id;
            if (userId == null)
                throw new UnauthorizedAccessException("Lỗi bảo mật: Không tìm thấy thông tin.");

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            if (!string.IsNullOrWhiteSpace(input.Email))
                await _userManager.SetEmailAsync(user, input.Email);

            if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
                await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);

            await _userManager.UpdateAsync(user);
        }
    }
}   
