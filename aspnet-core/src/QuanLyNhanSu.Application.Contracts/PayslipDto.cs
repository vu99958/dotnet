using System;
using Volo.Abp.Application.Dtos;

namespace QuanLyNhanSu
{
    public class PayslipDto : EntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int StandardWorkDays { get; set; }
        public double ActualWorkDays { get; set; }
        public int ApprovedLeaveDays { get; set; }
        public int OvertimeDays { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal TotalPenalty { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
    }
}
