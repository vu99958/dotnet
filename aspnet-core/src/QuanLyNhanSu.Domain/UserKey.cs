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

    /// <summary>
    /// ID chi nhánh phân bổ (Geofencing)
    /// </summary>
    public Guid? BranchId { get; set; }

    public UserKey() { }

    public UserKey(Guid id, Guid userId, string key, string role, string? description = null) : base(id)
    {
        UserId = userId;
        Key = key;
        Role = role;
        Description = description;
        Status = "active";
    }

    /* 
     * [ONBOARDING COMMENT - FOR JUNIOR DEV]
     * Tại sao lại đặt logic 'Revoke' ở đây?
     * Theo nguyên lý Rich Domain Model của DDD, Aggregate Root (UserKey) phải tự quản lý trạng thái của chính nó.
     * Cấm việc AppService tự gán giá trị: userKey.Status = "revoked";
     * Thay vào đó, gọi hàm userKey.RevokeKey() để bảo toàn tính đóng gói (Encapsulation).
     */
    public void RevokeKey()
    {
        if (Status == "revoked")
            throw new InvalidOperationException("Key này đã bị vô hiệu hóa trước đó!");
        
        Status = "revoked";
    }
}
