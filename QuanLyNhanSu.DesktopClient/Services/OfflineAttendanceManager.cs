using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuanLyNhanSu.DesktopClient.Services
{
    public class OfflineAttendanceRecord
    {
        public int Id { get; set; } // Added for SQLite Primary Key
        public string Action { get; set; } = ""; // "check-in" hoặc "check-out"
        public DateTime Timestamp { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class OfflineDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<OfflineAttendanceRecord> OfflineRecords { get; set; } = null!;

        protected override void OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineAttendance.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OfflineAttendanceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }

    public static class OfflineAttendanceManager
    {
        // [ONBOARDING COMMENT]: Đảm bảo DB được tạo khi sử dụng lần đầu tiên (Tự động migrate bằng SQLite)
        public static void InitializeDatabase()
        {
            using var db = new OfflineDbContext();
            db.Database.EnsureCreated();
        }

        public static void SaveRecord(OfflineAttendanceRecord record)
        {
            using var db = new OfflineDbContext();
            // Đảm bảo ACID và Thread-Safe khi lưu offline
            db.OfflineRecords.Add(record);
            db.SaveChanges();
        }

        public static List<OfflineAttendanceRecord> GetRecords()
        {
            try
            {
                using var db = new OfflineDbContext();
                return db.OfflineRecords.ToList();
            }
            catch
            {
                return new List<OfflineAttendanceRecord>();
            }
        }

        public static void ClearRecords()
        {
            using var db = new OfflineDbContext();
            // Xóa tất cả các bản ghi sau khi đã đồng bộ thành công
            db.OfflineRecords.RemoveRange(db.OfflineRecords);
            db.SaveChanges();
        }
    }
}
