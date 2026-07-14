using System;
using System.ComponentModel.DataAnnotations;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    public class CreateUpdateLeaveRequestDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    }
}
