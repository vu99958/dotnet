using System;
using Volo.Abp.Application.Dtos;

namespace QuanLyNhanSu
{
    public class PayslipComplaintDto : EntityDto<Guid>
    {
        public Guid PayslipId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string AdminReply { get; set; }
        public DateTime CreationTime { get; set; }
    }

    public class CreateComplaintDto
    {
        public Guid PayslipId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Reason { get; set; }
    }

    public class ResolveComplaintDto
    {
        public string Status { get; set; } // "Resolved" hoặc "Rejected"
        public string AdminReply { get; set; }
    }
}
