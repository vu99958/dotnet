using System;
using Volo.Abp.Domain.Entities;

namespace QuanLyNhanSu
{
    // Khuôn mẫu này sẽ tạo ra 1 bảng trong SQL Server
    public class AttendanceRecord : Entity<Guid>
    {
        // ID của nhân viên thực hiện chấm công
        public Guid UserId { get; set; }

        // Ngày đi làm (VD: 20/06/2026)
        public DateTime WorkDate { get; set; }

        // Giờ bấm Check-in (Có thể rỗng nếu chưa bấm)
        public DateTime? CheckInTime { get; set; }

        // Giờ bấm Check-out (Có thể rỗng nếu chưa tan làm)
        public DateTime? CheckOutTime { get; set; }

        // Trạng thái: "Đúng giờ", "Đi trễ", "Vắng mặt"...
        public string Status { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }

        // Hàm khởi tạo mặc định (bắt buộc phải có cho Entity Framework)
        protected AttendanceRecord() { }

        // Hàm khởi tạo có tham số để tạo bản ghi mới
        public AttendanceRecord(Guid id, Guid userId, DateTime workDate, string status = "Chưa rõ") 
            : base(id)
        {
            UserId = userId;
            WorkDate = workDate.Date; // Chỉ lấy phần ngày, bỏ phần giờ phút
            Status = status;
        }
    }
}