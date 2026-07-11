using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuanLyNhanSu.DesktopClient.Services
{
    public class OfflineAttendanceRecord
    {
        public string Action { get; set; } = ""; // "check-in" hoặc "check-out"
        public DateTime Timestamp { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public static class OfflineAttendanceManager
    {
        private static readonly string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineAttendance.json");

        public static void SaveRecord(OfflineAttendanceRecord record)
        {
            var records = GetRecords();
            records.Add(record);
            File.WriteAllText(filePath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static List<OfflineAttendanceRecord> GetRecords()
        {
            if (!File.Exists(filePath))
                return new List<OfflineAttendanceRecord>();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<OfflineAttendanceRecord>>(json) ?? new List<OfflineAttendanceRecord>();
            }
            catch
            {
                return new List<OfflineAttendanceRecord>();
            }
        }

        public static void ClearRecords()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
