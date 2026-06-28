using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace QuanLyNhanSu.Domain
{
    /// <summary>
    /// Entity đại diện cho 1 chi nhánh công ty.
    /// Mỗi chi nhánh có tọa độ GPS và bán kính hợp lệ để xác thực Geofencing.
    /// </summary>
    public class Branch : AuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// Tên chi nhánh (VD: "Trụ sở chính - Vĩnh Long")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Vĩ độ (Latitude) của chi nhánh
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Kinh độ (Longitude) của chi nhánh
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Bán kính cho phép chấm công (đơn vị: mét)
        /// </summary>
        public int RadiusInMeters { get; set; }

        // Hàm khởi tạo mặc định (bắt buộc cho Entity Framework)
        protected Branch() { }

        // Hàm khởi tạo có tham số
        public Branch(Guid id, string name, double latitude, double longitude, int radiusInMeters)
            : base(id)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            RadiusInMeters = radiusInMeters;
        }
    }
}
