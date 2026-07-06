using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    // Bản cam kết: Báo cho hệ thống biết Phân hệ này có 3 chức năng
    public interface IAttendanceAppService : IApplicationService
    {
        // 1. Hàm ghi nhận giờ vào làm (có xác thực vị trí Geofencing)
        Task<string> CheckInAsync(double userLat, double userLng);

        // 2. Hàm ghi nhận giờ tan làm (WinForms/Web cũ)
        Task<string> CheckOutAsync();

        // 👉 3. API ĐỒNG BỘ DỮ LIỆU TỪ MÁY CHẤM CÔNG (BẮT BUỘC PHẢI CÓ DÒNG NÀY)
        Task<int> SyncBulkDataAsync(List<SyncAttendanceDto> inputList);
        // 👉 THÊM DÒNG NÀY: API trả về báo cáo chấm công cho giao diện HR
        Task<List<AttendanceReportDto>> GetDailyReportAsync(string date);

        // 👉 THÊM DÒNG NÀY: API Xóa/Hủy chấm công dành cho Admin
        Task DeleteDailyAttendanceAsync(string userName, string date);

        // 👉 API BÁO CÁO TỔNG HỢP THEO THÁNG (Dùng để chốt lương)
        Task<List<MonthlyAttendanceDto>> GetMonthlyReportAsync(string fromDate, string toDate);
    }
}