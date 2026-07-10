using System;

namespace QuanLyNhanSu
{
    public class SyncAttendanceDto
    {
        // Đổi thành UserName và gán giá trị mặc định để xóa cảnh báo vàng
        public string UserName { get; set; } = string.Empty; 

        // Đổi thành TimeStamp cho khớp logic tính toán
        public DateTime TimeStamp { get; set; } 

        // Gán giá trị mặc định để xóa cảnh báo vàng
        public string CheckType { get; set; } = string.Empty; 

        // Phương thức xác thực từ máy chấm công: "Fingerprint", "Face", "Password", "Card"
        public string VerifyMethod { get; set; } = string.Empty;

        // Mã nhân viên trên máy chấm công (EnrollNumber)
        public string DeviceUserId { get; set; } = string.Empty;
    }
}