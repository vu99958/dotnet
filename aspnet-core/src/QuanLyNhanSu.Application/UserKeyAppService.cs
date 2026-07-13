using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Application.Contracts;
using QuanLyNhanSu.Helpers;
using QuanLyNhanSu.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace QuanLyNhanSu.Application;

/// <summary>
/// Application Service quản lý User Key
/// </summary>
[Authorize]
public class UserKeyAppService : ApplicationService, ITransientDependency
{
    private readonly IRepository<UserKey, Guid> _userKeyRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UserKeyAppService(
        IRepository<UserKey, Guid> userKeyRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _userKeyRepository = userKeyRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// Tạo user key mới với xử lý async, rollback và chặn tạo nhiều Key
    /// </summary>
    [UnitOfWork]
    public virtual async Task<UserKeyResultDto> CreateUserKeyAsync(CreateUserKeyDto input)
    {
        if (CurrentUser?.Id == null)
            throw new InvalidOperationException("DEBUG: CurrentUser.Id đang NULL — lỗi nằm ở cấu hình JWT claims, không phải ở logic tạo key.");
            
        // Validate role — Chấp nhận cả "super_admin" (legacy) và "superadmin" (chuẩn hệ thống)
        var validRoles = new[] { "user", "admin", "super_admin", "superadmin" };
        if (!Array.Exists(validRoles, element => element == input.Role))
        {
            throw new InvalidOperationException($"Role '{input.Role}' không hợp lệ. Các role hợp lệ: user, admin, superadmin");
        }

        var userId = CurrentUser!.Id!.Value;

        // ---------------------------------------------------------
        // CHỐT CHẶN BẢO MẬT: Kiểm tra xem User đã có Key nào chưa
        // ---------------------------------------------------------
        var existingKey = await _userKeyRepository.FirstOrDefaultAsync(x => x.UserId == userId);
        if (existingKey != null)
        {
            // Ném lỗi thẳng ra ngoài, WinForms sẽ bắt được và báo màu đỏ
            throw new InvalidOperationException($"Bạn đã có 1 Key mang quyền [{existingKey.Role.ToUpper()}] đang hoạt động! Mỗi tài khoản chỉ được phép sở hữu 1 Key duy nhất.");
        }

        // Tạo key duy nhất (UUID)
        var key = CryptoHelper.GenerateSecureKey(16);

        try
        {
            // Bắt đầu transaction
            using (var uow = _unitOfWorkManager.Begin(isTransactional: true))
            {
                var userKey = new UserKey(
                    id: GuidGenerator.Create(),
                    userId: userId,
                    key: key,
                    role: input.Role,
                    description: input.Description
                )
                {
                    ExpirationDate = input.ExpirationDate
                };

                // Lưu vào database
                await _userKeyRepository.InsertAsync(userKey);

                // Commit transaction
                await uow.CompleteAsync();

                return new UserKeyResultDto
                {
                    Id = userKey.Id,
                    UserId = userKey.UserId,
                    Key = userKey.Key,
                    Role = userKey.Role,
                    Description = userKey.Description,
                    Status = userKey.Status,
                    ExpirationDate = userKey.ExpirationDate,
                    CreationTime = userKey.CreationTime
                };
            }
        }
        catch (Exception ex)
        {
            // Tự động rollback khi có lỗi
            Logger.LogError($"Lỗi tạo User Key: {ex.Message}");
            throw new InvalidOperationException($"Không thể tạo User Key. Chi tiết: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lấy user key hiện tại của user
    /// </summary>
    public virtual async Task<List<UserKeyResultDto>> GetUserKeysAsync()
    {
        var userId = CurrentUser!.Id!.Value;
        var userKeys = await _userKeyRepository.GetListAsync(x => x.UserId == userId);

        var result = new List<UserKeyResultDto>();
        foreach (var uk in userKeys)
        {
            result.Add(new UserKeyResultDto
            {
                Id = uk.Id,
                UserId = uk.UserId,
                Key = uk.Key,
                Role = uk.Role,
                Description = uk.Description,
                Status = uk.Status,
                ExpirationDate = uk.ExpirationDate,
                CreationTime = uk.CreationTime
            });
        }

        return result;
    }

    /// <summary>
    /// Xác minh key hợp lệ
    /// </summary>
    [Authorize(QuanLyNhanSuPermissions.UserKey.Manage)]
    public virtual async Task<UserKeyResultDto?> VerifyKeyAsync(string key)
    {
        var userKey = await _userKeyRepository.FirstOrDefaultAsync(x => 
            x.Key == key && x.Status == "active");

        if (userKey == null)
            return null;

        // Kiểm tra hạn sử dụng
        if (userKey.ExpirationDate.HasValue && userKey.ExpirationDate.Value < DateTime.UtcNow)
            return null;

        return new UserKeyResultDto
        {
            Id = userKey.Id,
            UserId = userKey.UserId,
            Key = userKey.Key,
            Role = userKey.Role,
            Description = userKey.Description,
            Status = userKey.Status,
            ExpirationDate = userKey.ExpirationDate,
            CreationTime = userKey.CreationTime
        };
    }

    /// <summary>
    /// Xóa user key
    /// </summary>
    public virtual async Task DeleteUserKeyAsync(Guid id)
    {
        var userKey = await _userKeyRepository.GetAsync(id);
        
        if (userKey.UserId != CurrentUser!.Id!.Value)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa key này");

        await _userKeyRepository.DeleteAsync(userKey);
    }
}
