using System;
using Volo.Abp.Application.Dtos;

namespace QuanLyNhanSu
{
    /// <summary>
    /// DTO hiển thị thông tin cấu hình lương (kèm tên nhân viên từ bảng User)
    /// </summary>
    public class SalaryProfileDto : EntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
    }

    /// <summary>
    /// DTO nhận dữ liệu đầu vào khi Admin tạo hoặc cập nhật cấu hình lương
    /// </summary>
    public class CreateUpdateSalaryProfileDto
    {
        public Guid UserId { get; set; }
        public string Position { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
    }
}
