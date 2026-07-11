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
            // BUG-07 FIX: Chờ ngắn để EmployeeAppService.CreateAccountAsync insert key trước (nếu có).
            // Nếu EmployeeAppService đã tạo key (với role đúng từ Admin), ta bỏ qua.
            await Task.Delay(500);
            
            // Kiểm tra xem đã có key chưa (phòng hờ race condition với EmployeeAppService)
            var existingKey = await _userKeyRepository.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (existingKey == null)
            {
                var keyString = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
                
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
            else
            {
                _logger.LogInformation($"UserKey already exists for user {user.UserName} ({user.Id}), skipping auto-generation.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error auto-generating UserKey for user {user.UserName} ({user.Id}).");
        }
    }
}
