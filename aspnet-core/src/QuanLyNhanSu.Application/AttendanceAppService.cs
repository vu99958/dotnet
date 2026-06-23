using System;
using System.Threading.Tasks;
using Volo.Abp;
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
        // 1. XỬ LÝ CHECK-IN (Giờ vào làm - 08:00)
        // ==========================================
        public async Task<string> CheckInAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) 
            {
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");
            }

            var today = DateTime.Now.Date; // Chỉ lấy phần ngày (00:00:00)
            var now = DateTime.Now;        // Lấy cả giờ phút giây
            var shiftStart = today.AddHours(8); // Ca làm quy định bắt đầu lúc 08:00

            // 1. Chốt chống Spam
            var existingRecord = await _attendanceRepository.FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkDate == today);

            if (existingRecord != null)
            {
                // Dùng throw để WinForms bắt được lỗi và nhảy vào nhánh màu Đỏ
                throw new UserFriendlyException("Hôm nay bạn đã điểm danh vào ca rồi, không thể bấm 2 lần!");
            }

            // 2. Tính toán đi trễ (So sánh với 8h sáng)
            string statusMessage = "Đúng giờ";
            int lateMinutes = 0;

            if (now > shiftStart)
            {
                lateMinutes = (int)(now - shiftStart).TotalMinutes;
                statusMessage = $"Đi trễ {lateMinutes} phút";
            }

            // 3. Khởi tạo Record mới
            var newRecord = new AttendanceRecord(
                GuidGenerator.Create(),
                userId.Value,
                today,         
                statusMessage
            );
            newRecord.CheckInTime = now;

            // 👉 4. Ghi nhận thời gian vi phạm vào thẳng cột trong CSDL
            newRecord.LateMinutes = lateMinutes;
            newRecord.EarlyLeaveMinutes = 0; // Sáng mới vào thì về sớm chắc chắn là 0

            // 5. Lưu xuống Database
            await _attendanceRepository.InsertAsync(newRecord);

            return $"Check-in thành công lúc {now:HH:mm} - Trạng thái: {statusMessage}";
        }

        // ==========================================
        // 2. XỬ LÝ CHECK-OUT (Giờ tan làm - 17:00)
        // ==========================================
        public async Task<string> CheckOutAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) 
            {
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");
            }

            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            var shiftEnd = today.AddHours(17); // Ca làm quy định kết thúc lúc 17:00

            // 1. Tìm bản ghi Check-in lúc sáng của người này
            var record = await _attendanceRepository.FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkDate == today);

            if (record == null)
            {
                throw new UserFriendlyException("Sáng nay bạn chưa Check-in vào ca, nên không thể Check-out!");
            }

            // 2. Chốt chống Spam Check-out
            if (record.CheckOutTime != null)
            {
                throw new UserFriendlyException("Hôm nay bạn đã xác nhận tan làm rồi!");
            }

            // 3. Tính toán về sớm (So sánh với 17h chiều)
            string statusMessage = "Hoàn thành ca";
            int earlyMinutes = 0;

            if (now < shiftEnd)
            {
                earlyMinutes = (int)(shiftEnd - now).TotalMinutes;
                statusMessage = $"Về sớm {earlyMinutes} phút";
            }

            // 4. Cập nhật dữ liệu
            record.CheckOutTime = now;
            record.Status += $" | {statusMessage}"; // Nối thêm thông báo chiều vào thông báo sáng
            
            // 👉 5. Ghi nhận số phút về sớm vào thẳng cột trong CSDL
            record.EarlyLeaveMinutes = earlyMinutes;

            // 6. Lưu xuống Database
            await _attendanceRepository.UpdateAsync(record);

            return $"Check-out lúc {now:HH:mm}. {statusMessage} - Chúc buổi tối vui vẻ!";
        }
    }
}