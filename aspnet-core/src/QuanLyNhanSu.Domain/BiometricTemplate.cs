using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace QuanLyNhanSu.Domain
{
    /// <summary>
    /// Entity lưu trữ dữ liệu sinh trắc học (vân tay / khuôn mặt) của nhân viên.
    /// 
    /// Mục đích: Lưu tập trung trên Server để hỗ trợ đồng bộ chéo giữa các máy chấm công.
    /// VD: Nhân viên đăng ký vân tay ở chi nhánh A → Server lưu trữ → Đẩy sang máy chi nhánh B.
    /// 
    /// Mỗi nhân viên có thể có tối đa 10 mẫu vân tay (FingerIndex 0-9) và 1 mẫu khuôn mặt.
    /// </summary>
    public class BiometricTemplate : AggregateRoot<Guid>
    {
        /// <summary>
        /// ID của nhân viên trong hệ thống (FK đến IdentityUser).
        /// Có thể null nếu chưa map được từ EnrollNumber sang UserId.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Mã nhân viên trên máy chấm công (EnrollNumber).
        /// Đây là key chính để đồng bộ giữa các máy.
        /// </summary>
        public string EnrollNumber { get; set; } = null!;

        /// <summary>
        /// Loại sinh trắc học: "Fingerprint" hoặc "Face"
        /// </summary>
        public string TemplateType { get; set; } = null!;

        /// <summary>
        /// Chỉ số ngón tay (0-9). Đặt -1 nếu là khuôn mặt.
        /// 0=Ngón cái trái, 1=Ngón trỏ trái, ..., 5=Ngón cái phải, ...
        /// </summary>
        public int FingerIndex { get; set; }

        /// <summary>
        /// Dữ liệu mẫu sinh trắc học (chuỗi template gốc từ SDK ZKTeco).
        /// Đối với vân tay: chuỗi hex/base64 từ SSR_GetUserTmpStr().
        /// Đối với khuôn mặt: chuỗi hex/base64 từ GetUserFaceStr().
        /// </summary>
        public string TemplateData { get; set; } = null!;

        /// <summary>
        /// Kích thước dữ liệu template (byte)
        /// </summary>
        public int TemplateLength { get; set; }

        /// <summary>
        /// Serial Number của thiết bị đã đăng ký mẫu gốc (dùng để truy vết nguồn)
        /// </summary>
        public string? SourceDeviceSerial { get; set; }

        /// <summary>
        /// Thời điểm đăng ký mẫu sinh trắc học
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        // Hàm khởi tạo mặc định (bắt buộc cho Entity Framework)
        protected BiometricTemplate() { }

        // Hàm khởi tạo có tham số
        public BiometricTemplate(Guid id, string enrollNumber, string templateType, int fingerIndex,
            string templateData, int templateLength, string? sourceDeviceSerial = null, Guid? userId = null)
            : base(id)
        {
            EnrollNumber = enrollNumber;
            TemplateType = templateType;
            FingerIndex = fingerIndex;
            TemplateData = templateData;
            TemplateLength = templateLength;
            SourceDeviceSerial = sourceDeviceSerial;
            UserId = userId;
            RegisteredAt = DateTime.Now;
        }
    }
}
