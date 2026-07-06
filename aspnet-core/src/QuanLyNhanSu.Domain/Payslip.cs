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

        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }

        protected Payslip() { }

        public Payslip(Guid id, Guid userId, int month, int year,
            int standardWorkDays, double actualWorkDays, int approvedLeaveDays,
            int overtimeDays, decimal overtimePay,
            decimal totalPenalty, decimal grossSalary, decimal netSalary)
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
            GrossSalary = grossSalary;
            NetSalary = netSalary;
        }
    }
}
