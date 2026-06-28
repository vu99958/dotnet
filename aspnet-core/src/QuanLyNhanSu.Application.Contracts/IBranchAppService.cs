using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Giao diện quản lý chi nhánh (CRUD đầy đủ)
    /// </summary>
    public interface IBranchAppService : IApplicationService
    {
        /// <summary>
        /// Lấy danh sách tất cả chi nhánh
        /// </summary>
        Task<List<BranchDto>> GetListAsync();

        /// <summary>
        /// Tạo chi nhánh mới
        /// </summary>
        Task<BranchDto> CreateAsync(CreateUpdateBranchDto input);

        /// <summary>
        /// Cập nhật thông tin chi nhánh
        /// </summary>
        Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto input);

        /// <summary>
        /// Xóa chi nhánh theo ID
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
