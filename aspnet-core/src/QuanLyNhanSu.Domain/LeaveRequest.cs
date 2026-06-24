using System;
using Volo.Abp.Domain.Entities;

namespace QuanLyNhanSu
{
    public class LeaveRequest : Entity<Guid>
    {
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }

        protected LeaveRequest() { }

        public LeaveRequest(Guid id, Guid userId, DateTime startDate, DateTime endDate, string reason, string status = "Pending") 
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
