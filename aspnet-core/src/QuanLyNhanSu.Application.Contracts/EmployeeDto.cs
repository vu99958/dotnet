using System;

namespace QuanLyNhanSu
{
    // Khuôn xuất dữ liệu (Cũ)
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreationTime { get; set; }
    }

    // Khuôn Thêm mới nhân viên
    public class CreateEmployeeDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    // Khuôn Chỉnh sửa nhân viên
    public class UpdateEmployeeDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
    }
    // Khuôn chứa dữ liệu thống kê cho Bảng điều khiển
    public class DashboardStatsDto
    {
        public int TotalEmployees { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalUsers { get; set; }
    }
}