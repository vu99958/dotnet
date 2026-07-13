using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuanLyNhanSu.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using QuanLyNhanSu.Helpers;

namespace QuanLyNhanSu.Handlers;

/// <summary>
/// Lắng nghe sự kiện khi một IdentityUser được tạo mới để tự động cấp UserKey.
/// </summary>
public class UserCreatedEventHandler : ILocalEventHandler<EntityCreatedEventData<IdentityUser>>, ITransientDependency
{
    private readonly IRepository<UserKey, Guid> _userKeyRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(
        IRepository<UserKey, Guid> userKeyRepository,
        IGuidGenerator guidGenerator,
        ILogger<UserCreatedEventHandler> logger)
    {
        _userKeyRepository = userKeyRepository;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    public async Task HandleEventAsync(EntityCreatedEventData<IdentityUser> eventData)
    {
        var user = eventData.Entity;

        try
        {
            var keyString = CryptoHelper.GenerateSecureKey(16);
            
            var userKey = new UserKey(
                id: _guidGenerator.Create(),
                userId: user.Id,
                key: keyString,
                role: "user",
                description: "Auto-generated key for new user"
            );

            await _userKeyRepository.InsertAsync(userKey);
            _logger.LogInformation($"Successfully generated UserKey for user {user.UserName} ({user.Id}).");
        }
        catch (Exception ex)
        {
            if (ex.InnerException?.Message?.Contains("IX_UserKeys_UserId") == true || ex.Message.Contains("IX_UserKeys_UserId"))
            {
                // DESIGN-02: Bỏ qua nếu có DbUpdateException (báo hiệu Key đã được EmployeeAppService tạo trước đó)
                // Nhờ Unique Index trên UserId, Database sẽ bảo vệ chúng ta khỏi việc tạo trùng lặp.
                _logger.LogInformation($"UserKey already exists (Unique Constraint Violation) for user {user.UserName} ({user.Id}), skipping auto-generation.");
            }
            else 
            {
                _logger.LogError(ex, $"Error auto-generating UserKey for user {user.UserName} ({user.Id}).");
            }
        }
    }
}
