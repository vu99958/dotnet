using System;

namespace QuanLyNhanSu
{
    /// <summary>
    /// DTO trả về thông tin chi nhánh cho giao diện
    /// </summary>
    public class BranchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusInMeters { get; set; }
    }

    /// <summary>
    /// DTO nhận dữ liệu khi Thêm/Sửa chi nhánh
    /// </summary>
    public class CreateUpdateBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusInMeters { get; set; }
    }
}
