using System;

namespace QuanLyNhanSu.Payroll.BackgroundJobs
{
    public class PayrollCalculationJobArgs
    {
        public Guid? TenantId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
