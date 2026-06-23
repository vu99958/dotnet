using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    // Bản cam kết: Báo cho hệ thống biết Phân hệ này có 3 chức năng
    public interface IAttendanceAppService : IApplicationService
    {
        // 1. Hàm ghi nhận giờ vào làm (WinForms/Web cũ)
        Task<string> CheckInAsync();

        // 2. Hàm ghi nhận giờ tan làm (WinForms/Web cũ)
        Task<string> CheckOutAsync();

        // 👉 3. API ĐỒNG BỘ DỮ LIỆU TỪ MÁY CHẤM CÔNG (BẮT BUỘC PHẢI CÓ DÒNG NÀY)
        Task<int> SyncBulkDataAsync(List<SyncAttendanceDto> inputList);
        // 👉 THÊM DÒNG NÀY: API trả về báo cáo chấm công cho giao diện HR
        Task<List<AttendanceReportDto>> GetDailyReportAsync(DateTime date);
    }
}