using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace QuanLyNhanSu.Events
{
    [EventName("QuanLyNhanSu.Attendance.BulkSyncRequested")]
    public class BulkSyncRequestedEvent
    {
        public List<SyncAttendanceDto> AttendanceData { get; set; } = new List<SyncAttendanceDto>();
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }
}
