using System;
using Volo.Abp.Application.Dtos;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    public class LeaveRequestDto : EntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveRequestStatus Status { get; set; }
    }
}
