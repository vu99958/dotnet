using System;

namespace QuanLyNhanSu.Application.Contracts;

/// <summary>
/// DTO để tạo user key
/// </summary>
public class CreateUserKeyDto
{
    /// <summary>
    /// Vai trò: user, admin, super_admin
    /// </summary>
    public string Role { get; set; } = null!;
    
    /// <summary>
    /// Mô tả key
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Ngày hết hạn (null = không hết hạn)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// DTO return khi tạo user key thành công
/// </summary>
public class UserKeyResultDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Key { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreationTime { get; set; }
}
