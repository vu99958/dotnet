using System;

namespace QuanLyNhanSu
{
    public class MyProfileDto
    {
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Roles { get; set; }
        public string? BranchName { get; set; }
        public DateTime CreationTime { get; set; }
    }

    /// <summary>
    /// DTO để cập nhật thông tin cá nhân (BUG-08 FIX)
    /// </summary>
    public class UpdateMyProfileDto
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}