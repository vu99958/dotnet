using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    /// <summary>
    /// API thống kê cho Dashboard (chỉ Admin được gọi).
    /// </summary>
    public interface IDashboardAppService : IApplicationService
    {
        /// <summary>
        /// Trả về số nhân viên đi Đúng giờ và Đi trễ/Về sớm của ngày hôm nay.
        /// </summary>
        Task<TodayAttendanceStatsDto> GetTodayAttendanceStatsAsync();

        /// <summary>
        /// Trả về tổng quỹ lương Net theo từng tháng trong năm hiện tại.
        /// </summary>
        Task<List<MonthlySalaryStatsDto>> GetMonthlySalaryStatsAsync();
    }
}
