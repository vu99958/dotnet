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
    }
}