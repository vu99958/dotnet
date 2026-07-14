using System;
using Volo.Abp.Domain.Entities;

namespace QuanLyNhanSu
{
    public class Payslip : Entity<Guid>
    {
        public Guid UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // Ngày công chuẩn (tính tự động theo lịch tháng, loại trừ T7 CN)
        public int StandardWorkDays { get; set; }

        // Số ngày đi làm thực tế (trong giờ hành chính)
        public double ActualWorkDays { get; set; }

        // Số ngày nghỉ có phép (được hưởng lương)
        public int ApprovedLeaveDays { get; set; }

        // Số ngày tăng ca (vượt quá ngày công chuẩn)
        public int OvertimeDays { get; set; }

        // Tiền tăng ca (150% lương ngày)
        public decimal OvertimePay { get; set; }

        // Tổng tiền phạt đi trễ/về sớm
        public decimal TotalPenalty { get; set; }

        // Tiền đóng BHXH (8% BHXH + 1.5% BHYT + 1% BHTN = 10.5% của lương cơ bản)
        public decimal SocialInsurance { get; set; }

        // Thuế Thu Nhập Cá Nhân (PIT - Tính theo biểu thuế lũy tiến từng phần)
        public decimal PersonalIncomeTax { get; set; }

        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }

        protected Payslip() { }

        public Payslip(Guid id, Guid userId, int month, int year,
            int standardWorkDays, double actualWorkDays, int approvedLeaveDays,
            int overtimeDays, decimal overtimePay,
            decimal totalPenalty, decimal socialInsurance, decimal personalIncomeTax,
            decimal grossSalary, decimal netSalary)
            : base(id)
        {
            UserId = userId;
            Month = month;
            Year = year;
            StandardWorkDays = standardWorkDays;
            ActualWorkDays = actualWorkDays;
            ApprovedLeaveDays = approvedLeaveDays;
            OvertimeDays = overtimeDays;
            OvertimePay = overtimePay;
            TotalPenalty = totalPenalty;
            SocialInsurance = socialInsurance;
            PersonalIncomeTax = personalIncomeTax;
            GrossSalary = grossSalary;
            NetSalary = netSalary;
        }
    }
}
