using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace QuanLyNhanSu
{
    // [Authorize] là chốt chặn bảo mật: Chỉ ai có Token/Key hợp lệ mới được gọi API này
    [Authorize] 
    public class MyProfileAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;

        public MyProfileAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task<MyProfileDto> GetMyProfileAsync()
        {
            // 1. Lấy ID của người đang đăng nhập từ hệ thống (qua Token)
            var userId = CurrentUser.Id;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Lỗi bảo mật: Không tìm thấy thông tin người dùng hiện tại.");
            }

            // 2. Truy vấn Database để lấy thông tin chi tiết
            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy tài khoản trong hệ thống.");
            }

            // 3. Lấy danh sách các quyền (Role) của user này
            var roles = await _userManager.GetRolesAsync(user);

            // 4. Đóng gói vào DTO và trả về cho WinForms
            return new MyProfileDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = string.Join(", ", roles), // Ghép các quyền lại cách nhau bằng dấu phẩy
                CreationTime = user.CreationTime
            };
        }
    }
}   