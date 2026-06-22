using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    // Bản cam kết: Báo cho hệ thống biết Phân hệ này có 2 chức năng
    public interface IAttendanceAppService : IApplicationService
    {
        // Hàm ghi nhận giờ vào làm
        Task<string> CheckInAsync();

        // Hàm ghi nhận giờ tan làm
        Task<string> CheckOutAsync();
    }
}