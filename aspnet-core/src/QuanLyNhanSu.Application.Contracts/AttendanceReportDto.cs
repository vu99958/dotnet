using System;

namespace QuanLyNhanSu
{
    public class AttendanceReportDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        
        // Dùng string để WinForms dễ hiển thị định dạng HH:mm
        public string? CheckInTime { get; set; } 
        public string? CheckOutTime { get; set; }
        
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }

        // Tọa độ GPS khi chấm công (Geofencing)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? BranchName { get; set; }
    }
}