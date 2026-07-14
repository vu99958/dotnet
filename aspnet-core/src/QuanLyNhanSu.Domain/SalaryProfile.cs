using System;
using Volo.Abp.Domain.Entities;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Cấu hình lương của từng nhân viên.
    /// Mỗi nhân viên có chức vụ và mức lương riêng biệt.
    /// </summary>
    public class SalaryProfile : Entity<Guid>
    {
        // ID của nhân viên
        public Guid UserId { get; set; }

        // Chức vụ: "Giám đốc", "Trưởng phòng", "Nhân viên"...
        public string Position { get; set; } = string.Empty;

        // Lương cơ bản (VNĐ)
        public decimal BaseSalary { get; set; }

        // Phụ cấp (VNĐ)
        public decimal Allowance { get; set; }

        protected SalaryProfile() { }

        public SalaryProfile(Guid id, Guid userId, string position, decimal baseSalary, decimal allowance)
            : base(id)
        {
            UserId = userId;
            Position = position;
            BaseSalary = baseSalary;
            Allowance = allowance;
        }
    }
}
