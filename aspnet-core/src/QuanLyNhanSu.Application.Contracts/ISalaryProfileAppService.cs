using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Giao diện quản lý cấu hình lương nhân viên
    /// </summary>
    public interface ISalaryProfileAppService : IApplicationService
    {
        /// <summary>
        /// Lấy danh sách cấu hình lương kèm tên nhân viên.
        /// Nhân viên chưa có cấu hình cũng được trả về (với Position/BaseSalary/Allowance = mặc định).
        /// </summary>
        Task<List<SalaryProfileDto>> GetListAsync();

        /// <summary>
        /// Tạo mới hoặc cập nhật cấu hình lương cho 1 nhân viên
        /// </summary>
        Task CreateOrUpdateAsync(CreateUpdateSalaryProfileDto input);
    }
}
