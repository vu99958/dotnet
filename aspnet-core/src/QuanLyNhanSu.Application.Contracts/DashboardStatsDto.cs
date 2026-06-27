using System.Collections.Generic;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Thống kê chấm công hôm nay: Đúng giờ vs Đi trễ/Về sớm.
    /// </summary>
    public class TodayAttendanceStatsDto
    {
        /// <summary>Số nhân viên đi đúng giờ (LateMinutes == 0 và EarlyLeaveMinutes == 0).</summary>
        public int OnTimeCount { get; set; }

        /// <summary>Số nhân viên đi trễ hoặc về sớm.</summary>
        public int LateOrEarlyCount { get; set; }
    }

    /// <summary>
    /// Thống kê quỹ lương Net theo từng tháng trong năm.
    /// </summary>
    public class MonthlySalaryStatsDto
    {
        /// <summary>Tháng (1-12).</summary>
        public int Month { get; set; }

        /// <summary>Tổng NetSalary toàn công ty trong tháng.</summary>
        public decimal TotalNetSalary { get; set; }
    }
}
