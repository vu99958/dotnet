using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace QuanLyNhanSu
{
    public class AttendanceAppService : QuanLyNhanSuAppService, IAttendanceAppService
    {
        // Gọi kho chứa bảng Điểm Danh lên
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;

        public AttendanceAppService(IRepository<AttendanceRecord, Guid> attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
        }

        // ==========================================
        // 1. XỬ LÝ CHECK-IN (Giờ vào làm)
        // ==========================================
        public async Task<string> CheckInAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) return "Lỗi: Bạn chưa đăng nhập!";

            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            // Kiểm tra xem hôm nay nhân viên này đã chấm công chưa
            var existingRecord = await _attendanceRepository.FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkDate == today);

            if (existingRecord != null)
            {
                return "Bạn đã Check-in hôm nay rồi, không thể bấm 2 lần!";
            }

            // Phân loại tự động: Sau 8h30 sáng bị tính là đi trễ
            string status = now.TimeOfDay > new TimeSpan(8, 30, 0) ? "Đi trễ" : "Đúng giờ";

            // Ghi dữ liệu xuống Database
            var newRecord = new AttendanceRecord(
                GuidGenerator.Create(),
                userId.Value,
                now,
                status
            );
            newRecord.CheckInTime = now;

            await _attendanceRepository.InsertAsync(newRecord);

            return $"Check-in thành công lúc {now:HH:mm} - Trạng thái: {status}";
        }

        // ==========================================
        // 2. XỬ LÝ CHECK-OUT (Giờ tan làm)
        // ==========================================
        public async Task<string> CheckOutAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) return "Lỗi: Bạn chưa đăng nhập!";

            var today = DateTime.Now.Date;

            // Tìm bản ghi Check-in lúc sáng của người này
            var record = await _attendanceRepository.FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkDate == today);

            if (record == null)
            {
                return "Bạn chưa Check-in sáng nay, không thể Check-out!";
            }

            if (record.CheckOutTime != null)
            {
                return "Bạn đã Check-out tan làm rồi!";
            }

            // Ghi nhận giờ tan làm và lưu lại
            record.CheckOutTime = DateTime.Now;
            await _attendanceRepository.UpdateAsync(record);

            return $"Check-out thành công lúc {record.CheckOutTime:HH:mm}. Chúc bạn buổi tối vui vẻ!";
        }
    }
}