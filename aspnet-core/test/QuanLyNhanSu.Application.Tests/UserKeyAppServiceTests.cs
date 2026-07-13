using System;
using System.Threading.Tasks;
using NSubstitute;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Application;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace QuanLyNhanSu.Application.Tests
{
    /*
     * [ONBOARDING COMMENT - FOR JUNIOR DEV]
     * Tại sao phải test AppService?
     * Unit Test giúp đảm bảo khi một người khác sửa code (VD: vô tình xóa Authorize hoặc quên Update DB), test sẽ fail ngay.
     * Ở đây ta dùng NSubstitute để mock (làm giả) các dependency như Repository thay vì cần Database thật.
     * Sử dụng thư viện Shouldly (x.ShouldBe()) giúp code test đọc giống như tiếng Anh giao tiếp.
     */
    public class UserKeyAppServiceTests
    {
        private readonly UserKeyAppService _userKeyAppService;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public UserKeyAppServiceTests()
        {
            _userKeyRepository = Substitute.For<IRepository<UserKey, Guid>>();
            var unitOfWorkManager = Substitute.For<Volo.Abp.Uow.IUnitOfWorkManager>();

            var guidGenerator = Substitute.For<IGuidGenerator>();
            guidGenerator.Create().Returns(Guid.NewGuid());
            
            var lazyServiceProvider = Substitute.For<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
            lazyServiceProvider.LazyGetService<IGuidGenerator>(Arg.Any<Func<IServiceProvider, object>>()).Returns(guidGenerator);
            lazyServiceProvider.LazyGetRequiredService<IGuidGenerator>().Returns(guidGenerator);

            _userKeyAppService = new UserKeyAppService(
                _userKeyRepository,
                unitOfWorkManager
            )
            {
                LazyServiceProvider = lazyServiceProvider
            };
        }

        [Fact]
        public async Task ResetUserKeyAsync_Should_Revoke_OldKey_And_Create_NewKey()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldKeyId = Guid.NewGuid();
            var oldKeyEntity = new UserKey(oldKeyId, userId, "OldHash", "user");
            
            // Setup Mock: Khi tìm key active thì trả về key cũ
            _userKeyRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserKey, bool>>>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<UserKey?>(oldKeyEntity));

            // Setup Mock: Bỏ qua hành động Insert và Update (chỉ để code chạy không lỗi)
            _userKeyRepository.UpdateAsync(Arg.Any<UserKey>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(oldKeyEntity));
                
            _userKeyRepository.InsertAsync(Arg.Any<UserKey>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(oldKeyEntity));

            // Act
            var plainNewKey = await _userKeyAppService.ResetUserKeyAsync(userId);

            // Assert
            plainNewKey.ShouldNotBeNullOrWhiteSpace();
            plainNewKey.Length.ShouldBe(16);

            // Xác minh: Key cũ phải bị gán trạng thái revoked (gọi hàm RevokeKey)
            oldKeyEntity.Status.ShouldBe("revoked");

            // Xác minh: Hàm UpdateAsync đã được gọi 1 lần cho key cũ
            await _userKeyRepository.Received(1).UpdateAsync(Arg.Is<UserKey>(x => x.Id == oldKeyId && x.Status == "revoked"), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>());

            // Xác minh: Hàm InsertAsync đã được gọi 1 lần để tạo key mới với role "user"
            await _userKeyRepository.Received(1).InsertAsync(Arg.Is<UserKey>(x => x.UserId == userId && x.Role == "user"), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>());
        }
    }
}
