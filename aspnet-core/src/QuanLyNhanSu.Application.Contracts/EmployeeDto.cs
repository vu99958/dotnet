using System;

namespace QuanLyNhanSu
{
    // Khuôn xuất dữ liệu (Cũ)
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
        public Guid? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    // Khuôn Thêm mới nhân viên
    public class CreateEmployeeDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
    }

    // Khuôn Chỉnh sửa nhân viên
    public class UpdateEmployeeDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
    }
    // Khuôn chứa dữ liệu thống kê cho Bảng điều khiển
    public class DashboardStatsDto
    {
        public int TotalEmployees { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalUsers { get; set; }
    }
}