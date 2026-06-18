using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace QuanLyNhanSu
{
    // Yêu cầu phải đăng nhập mới được gọi API này
    [Authorize] 
    public class EmployeeAppService : ApplicationService
    {
        // Sử dụng kho dữ liệu User mặc định của ABP
        private readonly IIdentityUserRepository _userRepository;

        public EmployeeAppService(IIdentityUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // Hàm lấy danh sách nhân viên
        public async Task<List<EmployeeDto>> GetListEmployeeAsync()
        {
            // 1. Lấy toàn bộ user từ Database
            var users = await _userRepository.GetListAsync();
            
            var result = new List<EmployeeDto>();

            // 2. Chuyển đổi dữ liệu thô sang DTO để trả về Frontend
            foreach (var user in users)
            {
                result.Add(new EmployeeDto
                {
                    Id = user.Id,
                    UserName = user.UserName.ToUpper(), // Viết hoa tên cho đẹp
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "Chưa cập nhật",
                    CreationTime = user.CreationTime
                });
            }

            return result;
        }
    }
}