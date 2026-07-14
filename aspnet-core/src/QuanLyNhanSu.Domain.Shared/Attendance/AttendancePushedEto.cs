using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace QuanLyNhanSu.Attendance
{
    // [ONBOARDING COMMENT]: Class con định nghĩa cấu trúc dữ liệu cho mỗi log chấm công trong ETO
    public class AttendanceLogEto
    {
        public string UserName { get; set; } = string.Empty; 
        public DateTime TimeStamp { get; set; } 
        public string CheckType { get; set; } = string.Empty; 
        public string VerifyMethod { get; set; } = string.Empty;
        public string DeviceUserId { get; set; } = string.Empty;
    }

    // [ONBOARDING COMMENT]: Class mang hậu tố Eto (Event Transfer Object). Định nghĩa cấu trúc gói tin bắn vào RabbitMQ.
    [EventName("QuanLyNhanSu.Attendance.Pushed")]
    public class AttendancePushedEto
    {
        public List<AttendanceLogEto> Logs { get; set; } = new List<AttendanceLogEto>();
        public string BranchName { get; set; } = string.Empty;
    }
}
