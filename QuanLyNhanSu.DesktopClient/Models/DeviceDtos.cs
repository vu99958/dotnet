namespace QuanLyNhanSu.DesktopClient.Models
{
    /// <summary>
    /// DTO đồng bộ log chấm công — gửi từ Desktop Client lên Backend API.
    /// Mapping 1:1 với SyncAttendanceDto trên Backend (Application.Contracts).
    /// Desktop Client không reference trực tiếp Backend project, nên cần bản sao riêng.
    /// </summary>
    public class SyncAttendanceClientDto
    {
        public string UserName { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string CheckType { get; set; } = string.Empty;
        public string VerifyMethod { get; set; } = string.Empty;
        public string DeviceUserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO lưu trữ mẫu sinh trắc học (vân tay / khuôn mặt).
    /// Dùng để truyền dữ liệu giữa Desktop Client ↔ Backend API.
    /// </summary>
    public class BiometricTemplateClientDto
    {
        /// <summary>Mã nhân viên trên máy chấm công (EnrollNumber)</summary>
        public string EnrollNumber { get; set; } = string.Empty;

        /// <summary>Tên nhân viên trên máy chấm công (nếu có)</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Loại: "Fingerprint" hoặc "Face"</summary>
        public string TemplateType { get; set; } = string.Empty;

        /// <summary>Chỉ số ngón tay (0-9). Đặt -1 nếu là khuôn mặt.</summary>
        public int FingerIndex { get; set; }

        /// <summary>Dữ liệu mẫu sinh trắc học (chuỗi gốc từ SDK)</summary>
        public string TemplateData { get; set; } = string.Empty;

        /// <summary>Kích thước dữ liệu template</summary>
        public int TemplateLength { get; set; }

        /// <summary>Serial Number của thiết bị nguồn (máy đã đăng ký gốc)</summary>
        public string SourceDeviceSerial { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO thông tin nhân viên đăng ký trên máy chấm công.
    /// Dùng khi liệt kê danh sách user trên thiết bị.
    /// </summary>
    public class DeviceUserInfoDto
    {
        /// <summary>Mã nhân viên trên máy chấm công</summary>
        public string EnrollNumber { get; set; } = string.Empty;

        /// <summary>Tên nhân viên (nếu đã đăng ký trên máy)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Quyền trên máy: 0=User, 3=Admin</summary>
        public int Privilege { get; set; }

        /// <summary>Số lượng mẫu vân tay đã đăng ký</summary>
        public int FingerprintCount { get; set; }

        /// <summary>Đã đăng ký khuôn mặt hay chưa</summary>
        public bool HasFaceTemplate { get; set; }
    }
}
