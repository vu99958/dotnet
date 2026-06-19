using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain;

namespace QuanLyNhanSu
{
    [Authorize] 
    public class EmployeeAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public EmployeeAppService(IdentityUserManager userManager, IIdentityUserRepository userRepository, IRepository<UserKey, Guid> userKeyRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _userKeyRepository = userKeyRepository;
        }

        // --- HÀM ẨN: Tra cứu Quyền hạn từ bảng UserKey ---
        private async Task<string> GetUserRoleAsync(Guid userId)
        {
            var key = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId);
            return key != null ? key.Role.ToLower() : "user";
        }

        // =====================================
        // 1. LẤY DANH SÁCH
        // =====================================
        public async Task<List<EmployeeDto>> GetListEmployeeAsync()
        {
            var users = await _userRepository.GetListAsync();
            var result = new List<EmployeeDto>();
            foreach (var user in users)
            {
                result.Add(new EmployeeDto {
                    Id = user.Id, UserName = user.UserName.ToUpper(), Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "Chưa cập nhật", CreationTime = user.CreationTime
                });
            }
            return result;
        }

        // =====================================
        // 2. THÊM NHÂN VIÊN & TỰ ĐỘNG CẤP KEY
        // =====================================
        public async Task CreateAccountAsync(CreateEmployeeDto input)
        {
            // FIX CS8629: Kiểm tra nếu chưa đăng nhập thì chặn lại ngay
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);
            
            if (myRole != "admin" && myRole != "superadmin") 
                throw new UserFriendlyException("Từ chối: Bạn không phải quản trị viên!");

            if ((input.Role.ToLower() == "admin" || input.Role.ToLower() == "superadmin") && myRole != "superadmin")
                throw new UserFriendlyException("Từ chối: Chỉ Super Admin mới được tạo tài khoản cấp quản lý!");

            // Tạo tài khoản trong bảng lõi ABP
            var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.Email);
            var result = await _userManager.CreateAsync(user, input.Password);
            
            if (!result.Succeeded) throw new UserFriendlyException("Lỗi: Trùng tên/email hoặc mật khẩu quá yếu.");

            // FIX CS1061: Dùng hàm của UserManager để set số điện thoại an toàn
            await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);

            // Tự động sinh Key và gắn Quyền vào bảng UserKey
            var newKey = new UserKey(GuidGenerator.Create(), user.Id, Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(), input.Role);
            await _userKeyRepository.InsertAsync(newKey);
        }

        // =====================================
        // 3. SỬA THÔNG TIN & SỬA QUYỀN
        // =====================================
        public async Task UpdateAccountAsync(Guid id, UpdateEmployeeDto input)
        {
            // FIX CS8629: Bắt lỗi rỗng
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);
            string targetRole = await GetUserRoleAsync(id);

            if (myRole != "admin" && myRole != "superadmin") throw new UserFriendlyException("Từ chối: Bạn không có quyền!");
            
            if (targetRole == "superadmin" && currentUserId != id) 
                throw new UserFriendlyException("Cảnh báo: Không được chạm vào Super Admin!");
            
            if (targetRole == "admin" && myRole != "superadmin" && currentUserId != id)
                throw new UserFriendlyException("Từ chối: Admin không được sửa hồ sơ của Admin khác!");

            if (input.Role.ToLower() == "admin" && myRole != "superadmin")
                throw new UserFriendlyException("Từ chối: Bạn không đủ quyền để nâng chức người này lên Admin!");

            var user = await _userManager.FindByIdAsync(id.ToString());
            // FIX CS8602: Kiểm tra User có tồn tại không trước khi sửa
            if (user == null) throw new UserFriendlyException("Không tìm thấy người dùng này!");

            // FIX CS1061: Sử dụng bộ UserManager chuẩn của Identity để cập nhật thông tin
            await _userManager.SetUserNameAsync(user, input.UserName);
            await _userManager.SetEmailAsync(user, input.Email);
            await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) throw new UserFriendlyException("Cập nhật thông tin gốc thất bại!");

            // Cập nhật Quyền
            var key = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == id);
            if (key != null) {
                key.Role = input.Role;
                await _userKeyRepository.UpdateAsync(key);
            }
        }

        // =====================================
        // 4. XÓA NHÂN VIÊN
        // =====================================
        public async Task DeleteAccountAsync(Guid id)
        {
            // FIX CS8629
            var currentUserId = CurrentUser.Id ?? throw new UnauthorizedAccessException("Bạn chưa đăng nhập!");
            string myRole = await GetUserRoleAsync(currentUserId);
            string targetRole = await GetUserRoleAsync(id);

            if (targetRole == "superadmin") throw new UserFriendlyException("Tuyệt đối không được xóa Super Admin!");
            if (targetRole == "admin" && myRole != "superadmin") throw new UserFriendlyException("Chỉ Super Admin mới được chém Admin!");

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null) {
                await _userManager.DeleteAsync(user); // Xóa gốc
                var key = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == id);
                if (key != null) await _userKeyRepository.DeleteAsync(key); // Xóa rễ
            }
        }
    }
}