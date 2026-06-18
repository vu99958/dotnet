// định nghĩa những cột thông tin nào sẽ được gửi về cho giao diện WinForms hiển thị
using System;

namespace QuanLyNhanSu
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreationTime { get; set; }
    }
}