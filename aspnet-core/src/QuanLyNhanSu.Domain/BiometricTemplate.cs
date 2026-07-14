using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using QuanLyNhanSu.Enums;

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
        public Guid? UserId { get; private set; }

        /// <summary>
        /// Mã nhân viên trên máy chấm công (EnrollNumber).
        /// Đây là key chính để đồng bộ giữa các máy.
        /// </summary>
        public string EnrollNumber { get; private set; } = string.Empty;

        /// <summary>
        /// Loại sinh trắc học: "Fingerprint" hoặc "Face"
        /// </summary>
        public BiometricType TemplateType { get; private set; }

        /// <summary>
        /// Chỉ số ngón tay (0-9). Đặt -1 nếu là khuôn mặt.
        /// 0=Ngón cái trái, 1=Ngón trỏ trái, ..., 5=Ngón cái phải, ...
        /// </summary>
        public int FingerIndex { get; private set; }

        /// <summary>
        /// Dữ liệu mẫu sinh trắc học (chuỗi template gốc từ SDK ZKTeco).
        /// Đối với vân tay: chuỗi hex/base64 từ SSR_GetUserTmpStr().
        /// Đối với khuôn mặt: chuỗi hex/base64 từ GetUserFaceStr().
        /// </summary>
        public string TemplateData { get; private set; } = string.Empty;

        /// <summary>
        /// Kích thước dữ liệu template (byte)
        /// </summary>
        public int TemplateLength { get; private set; }

        /// <summary>
        /// Serial Number của thiết bị đã đăng ký mẫu gốc (dùng để truy vết nguồn)
        /// </summary>
        public string? SourceDeviceSerial { get; private set; }

        /// <summary>
        /// Thời điểm đăng ký mẫu sinh trắc học
        /// </summary>
        public DateTime RegisteredAt { get; private set; }

        public void UpdateTemplate(string newData, int newLength, string? newSerial)
        {
            if (string.IsNullOrWhiteSpace(newData)) 
                throw new ArgumentException("Dữ liệu template không được để trống", nameof(newData));
            
            TemplateData = newData;
            TemplateLength = newLength;
            SourceDeviceSerial = newSerial;
            RegisteredAt = DateTime.Now;
        }

        // Hàm khởi tạo mặc định (bắt buộc cho Entity Framework)
        protected BiometricTemplate() { }

        // Hàm khởi tạo có tham số
        public BiometricTemplate(Guid id, string enrollNumber, BiometricType templateType, int fingerIndex,
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
