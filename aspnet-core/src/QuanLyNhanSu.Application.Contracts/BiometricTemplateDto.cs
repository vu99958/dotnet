using System;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    /// <summary>
    /// DTO truyền dữ liệu sinh trắc học giữa Client ↔ Backend API.
    /// Dùng cho cả upload (Client → Server) và download (Server → Client).
    /// </summary>
    public class BiometricTemplateDto
    {
        public Guid? Id { get; set; }

        /// <summary>ID nhân viên trong hệ thống (nullable nếu chưa map)</summary>
        public Guid? UserId { get; set; }

        /// <summary>Mã nhân viên trên máy chấm công</summary>
        public string EnrollNumber { get; set; } = string.Empty;

        /// <summary>Loại: Fingerprint hoặc Face</summary>
        public BiometricType TemplateType { get; set; }

        /// <summary>Chỉ số ngón tay (0-9), = -1 nếu Face</summary>
        public int FingerIndex { get; set; }

        /// <summary>Dữ liệu mẫu sinh trắc học (chuỗi gốc từ SDK)</summary>
        public string TemplateData { get; set; } = string.Empty;

        /// <summary>Kích thước dữ liệu template</summary>
        public int TemplateLength { get; set; }

        /// <summary>Serial thiết bị nguồn</summary>
        public string? SourceDeviceSerial { get; set; }

        /// <summary>Thời điểm đăng ký</summary>
        public DateTime RegisteredAt { get; set; }
    }
}
