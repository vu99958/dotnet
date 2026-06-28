using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using QuanLyNhanSu.Domain;

namespace QuanLyNhanSu.Data
{
    /// <summary>
    /// Tự động tạo 1 chi nhánh mặc định khi chạy DbMigrator lần đầu.
    /// ABP sẽ tự phát hiện class này nhờ implement IDataSeedContributor.
    /// </summary>
    public class BranchDataSeeder : IDataSeedContributor, ITransientDependency
    {
        private readonly IRepository<Branch, Guid> _branchRepository;
        private readonly IGuidGenerator _guidGenerator;

        public BranchDataSeeder(
            IRepository<Branch, Guid> branchRepository,
            IGuidGenerator guidGenerator)
        {
            _branchRepository = branchRepository;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // Kiểm tra nếu đã có chi nhánh thì không tạo thêm
            var count = await _branchRepository.CountAsync();
            if (count > 0) return;

            // Tạo chi nhánh mặc định: Trụ sở chính - Vĩnh Long
            var defaultBranch = new Branch(
                _guidGenerator.Create(),
                "Trụ sở chính - Vĩnh Long",
                10.2541,    // Latitude
                105.9723,   // Longitude
                500         // RadiusInMeters (500 mét)
            );

            await _branchRepository.InsertAsync(defaultBranch);
        }
    }
}
