using System;

namespace QuanLyNhanSu
{
    public class MyProfileDto
    {
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Roles { get; set; }
        public DateTime CreationTime { get; set; }
    }
}