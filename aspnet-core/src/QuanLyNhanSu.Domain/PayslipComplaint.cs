using System;
using Volo.Abp.Domain.Entities.Auditing;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    public class PayslipComplaint : FullAuditedEntity<Guid>
    {
        public Guid PayslipId { get; set; }
        public Guid UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveRequestStatus Status { get; set; } // Pending, Approved (Resolved), Rejected
        public string AdminReply { get; set; } = string.Empty;

        protected PayslipComplaint() { }

        public PayslipComplaint(Guid id, Guid payslipId, Guid userId, int month, int year, string reason)
        {
            Id = id;
            PayslipId = payslipId;
            UserId = userId;
            Month = month;
            Year = year;
            Reason = reason;
            Status = LeaveRequestStatus.Pending;
            AdminReply = "";
        }
    }
}
