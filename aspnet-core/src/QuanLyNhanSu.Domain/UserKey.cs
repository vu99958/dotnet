using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace QuanLyNhanSu.Domain;

/// <summary>
/// Entity lưu key phân loại tài khoản (user, admin, super admin)
/// </summary>
public class UserKey : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Mã key duy nhất
    /// </summary>
    public string Key { get; set; } = null!;
    
    /// <summary>
    /// Vai trò: user, admin, super_admin
    /// </summary>
    public string Role { get; set; } = null!;
    
    /// <summary>
    /// Mô tả key
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Trạng thái: active, inactive, revoked
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// Ngày hết hạn (null = không hết hạn)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    public UserKey() { }

    public UserKey(Guid id, Guid userId, string key, string role, string? description = null) : base(id)
    {
        UserId = userId;
        Key = key;
        Role = role;
        Description = description;
        Status = "active";
    }
}
