using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Service quản lý chi nhánh — CRUD đầy đủ
    /// </summary>
    public class BranchAppService : QuanLyNhanSuAppService, IBranchAppService
    {
        private readonly IRepository<Branch, Guid> _branchRepository;

        public BranchAppService(IRepository<Branch, Guid> branchRepository)
        {
            _branchRepository = branchRepository;
        }

        /// <summary>
        /// Lấy danh sách tất cả chi nhánh
        /// </summary>
        public async Task<List<BranchDto>> GetListAsync()
        {
            var branches = await _branchRepository.GetListAsync();

            return branches.Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                RadiusInMeters = b.RadiusInMeters
            }).OrderBy(b => b.Name).ToList();
        }

        /// <summary>
        /// Tạo chi nhánh mới
        /// </summary>
        public async Task<BranchDto> CreateAsync(CreateUpdateBranchDto input)
        {
            var branch = new Branch(
                GuidGenerator.Create(),
                input.Name,
                input.Latitude,
                input.Longitude,
                input.RadiusInMeters
            );

            await _branchRepository.InsertAsync(branch);

            return new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                RadiusInMeters = branch.RadiusInMeters
            };
        }

        /// <summary>
        /// Cập nhật thông tin chi nhánh
        /// </summary>
        public async Task<BranchDto> UpdateAsync(Guid id, CreateUpdateBranchDto input)
        {
            var branch = await _branchRepository.GetAsync(id);

            branch.Name = input.Name;
            branch.Latitude = input.Latitude;
            branch.Longitude = input.Longitude;
            branch.RadiusInMeters = input.RadiusInMeters;

            await _branchRepository.UpdateAsync(branch);

            return new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                RadiusInMeters = branch.RadiusInMeters
            };
        }

        /// <summary>
        /// Xóa chi nhánh theo ID
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _branchRepository.DeleteAsync(id);
        }
    }
}
