using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using QuanLyNhanSu.Permissions;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Service quản lý cấu hình lương cho từng nhân viên
    /// </summary>
    [Authorize]
    public class SalaryProfileAppService : QuanLyNhanSuAppService, ISalaryProfileAppService
    {
        private readonly IRepository<SalaryProfile, Guid> _salaryProfileRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public SalaryProfileAppService(
            IRepository<SalaryProfile, Guid> salaryProfileRepository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _salaryProfileRepository = salaryProfileRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Lấy danh sách TẤT CẢ nhân viên kèm cấu hình lương.
        /// Nhân viên chưa có cấu hình sẽ hiển thị giá trị mặc định (Chưa thiết lập, 0, 0).
        /// </summary>
        public async Task<List<SalaryProfileDto>> GetListAsync()
        {
            var users = await _userRepository.GetListAsync();
            var profiles = await _salaryProfileRepository.GetListAsync();

            // LEFT JOIN: Lấy tất cả user, kể cả chưa có SalaryProfile
            var result = (from u in users
                          join p in profiles on u.Id equals p.UserId into gj
                          from profile in gj.DefaultIfEmpty()
                          select new SalaryProfileDto
                          {
                              Id = profile?.Id ?? Guid.Empty,
                              UserId = u.Id,
                              UserName = u.Name ?? u.UserName,
                              Position = profile?.Position ?? "Chưa thiết lập",
                              BaseSalary = profile?.BaseSalary ?? 0,
                              Allowance = profile?.Allowance ?? 0
                          }).ToList();

            return result;
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật cấu hình lương cho 1 nhân viên.
        /// Nếu nhân viên đã có SalaryProfile → cập nhật.
        /// Nếu chưa có → tạo mới.
        /// </summary>
        public async Task CreateOrUpdateAsync(CreateUpdateSalaryProfileDto input)
        {
            // Tìm xem nhân viên này đã có cấu hình lương chưa
            var existing = (await _salaryProfileRepository.GetListAsync(x => x.UserId == input.UserId))
                           .FirstOrDefault();

            if (existing != null)
            {
                // Cập nhật cấu hình hiện có
                existing.Position = input.Position;
                existing.BaseSalary = input.BaseSalary;
                existing.Allowance = input.Allowance;
                await _salaryProfileRepository.UpdateAsync(existing);
            }
            else
            {
                // Tạo cấu hình mới cho nhân viên
                var newProfile = new SalaryProfile(
                    GuidGenerator.Create(),
                    input.UserId,
                    input.Position,
                    input.BaseSalary,
                    input.Allowance
                );
                await _salaryProfileRepository.InsertAsync(newProfile);
            }
        }
    }
}
