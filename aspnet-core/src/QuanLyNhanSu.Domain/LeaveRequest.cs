using System;
using Volo.Abp.Domain.Entities;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    public class LeaveRequest : Entity<Guid>
    {
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveRequestStatus Status { get; set; }

        protected LeaveRequest() { }

        public LeaveRequest(Guid id, Guid userId, DateTime startDate, DateTime endDate, string reason, LeaveRequestStatus status = LeaveRequestStatus.Pending) 
            : base(id)
        {
            UserId = userId;
            StartDate = startDate;
            EndDate = endDate;
            Reason = reason;
            Status = status;
        }
    }
}
