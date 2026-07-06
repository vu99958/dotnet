using System;

namespace QuanLyNhanSu
{
    /// <summary>
    /// DTO báo cáo tổng hợp chấm công theo tháng.
    /// Dùng để chốt lương cuối tháng, gửi cho Module Payroll.
    /// </summary>
    public class MonthlyAttendanceDto
    {
        // === THÔNG TIN NHÂN VIÊN ===
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string BranchName { get; set; } = "Không xác định";

        // === TỔNG HỢP NGÀY CÔNG ===

        /// <summary>
        /// Tổng số ngày đi làm (đã Check-in)
        /// </summary>
        public int TotalWorkDays { get; set; }

        /// <summary>
        /// Tổng số phút đi trễ trong tháng
        /// </summary>
        public int TotalLateMinutes { get; set; }

        /// <summary>
        /// Tổng số phút về sớm trong tháng
        /// </summary>
        public int TotalEarlyLeaveMinutes { get; set; }

        /// <summary>
        /// Tổng số ngày vắng mặt không phép (không Check-in, không có đơn xin nghỉ)
        /// </summary>
        public int TotalAbsentDays { get; set; }

        /// <summary>
        /// Tổng số ngày nghỉ có phép (có đơn LeaveRequest được duyệt)
        /// </summary>
        public int TotalLeaveDays { get; set; }

        /// <summary>
        /// Tổng số lần quên Check-out (có Check-in nhưng không có Check-out).
        /// HR dùng để xử lý phạt hoặc nhắc nhở nhân viên.
        /// </summary>
        public int TotalMissingCheckOuts { get; set; }
    }
}
