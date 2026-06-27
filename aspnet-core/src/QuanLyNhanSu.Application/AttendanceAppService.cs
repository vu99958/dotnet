using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity; 
using Microsoft.AspNetCore.Authorization; 
using QuanLyNhanSu.Domain;

namespace QuanLyNhanSu
{
    public class AttendanceAppService : QuanLyNhanSuAppService, IAttendanceAppService
    {
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;

        public AttendanceAppService(
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<UserKey, Guid> userKeyRepository) 
        {
            _attendanceRepository = attendanceRepository;
            _userRepository = userRepository;
            _userKeyRepository = userKeyRepository;
        }

        // ==========================================
        // 1. API ĐỒNG BỘ TỪ MÁY CHẤM CÔNG
        // ==========================================
        [AllowAnonymous] 
        public async Task<int> SyncBulkDataAsync(List<SyncAttendanceDto> inputList)
        {
            if (inputList == null || !inputList.Any()) return 0;

            var userNames = inputList.Select(x => x.UserName).Distinct().ToList();
            var users = await _userRepository.GetListAsync(x => userNames.Contains(x.UserName));
            var userDictionary = users.ToDictionary(x => x.UserName, x => x.Id);

            var listToSave = new List<AttendanceRecord>();
            var today = DateTime.Now.Date;
            var shiftStart = today.AddHours(8);  // 08:00
            var shiftEnd = today.AddHours(17);   // 17:00

            var groupedData = inputList.GroupBy(x => x.UserName).ToList();

            foreach (var group in groupedData)
            {
                if (!userDictionary.TryGetValue(group.Key, out Guid userId)) continue;

                var checkIn = group.Where(x => x.CheckType == "IN").OrderBy(x => x.TimeStamp).FirstOrDefault();
                var checkOut = group.Where(x => x.CheckType == "OUT").OrderByDescending(x => x.TimeStamp).FirstOrDefault();

                if (checkIn == null) continue;

                int lateMinutes = 0;
                int earlyMinutes = 0;
                string statusMessage = "Hệ thống tự động đồng bộ";

                if (checkIn.TimeStamp > shiftStart)
                {
                    lateMinutes = (int)(checkIn.TimeStamp - shiftStart).TotalMinutes;
                }

                if (checkOut != null && checkOut.TimeStamp < shiftEnd)
                {
                    earlyMinutes = (int)(shiftEnd - checkOut.TimeStamp).TotalMinutes;
                }

                var record = new AttendanceRecord(
                    GuidGenerator.Create(),
                    userId,
                    checkIn.TimeStamp.Date,
                    statusMessage
                );
                record.CheckInTime = checkIn.TimeStamp;
                record.CheckOutTime = checkOut?.TimeStamp;
                record.LateMinutes = lateMinutes;
                record.EarlyLeaveMinutes = earlyMinutes;

                listToSave.Add(record);
            }

            if (listToSave.Any())
            {
                await _attendanceRepository.InsertManyAsync(listToSave);
            }

            return listToSave.Count; 
        }

        // ==========================================
        // 2. XỬ LÝ CHECK-IN 
        // ==========================================
        public async Task<string> CheckInAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) 
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");

            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            var shiftStart = today.AddHours(8);

            var existingRecord = await _attendanceRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.WorkDate == today);
            if (existingRecord != null)
                throw new UserFriendlyException("Hôm nay bạn đã điểm danh vào ca rồi, không thể bấm 2 lần!");

            string statusMessage = "Đúng giờ";
            int lateMinutes = 0;

            if (now > shiftStart)
            {
                lateMinutes = (int)(now - shiftStart).TotalMinutes;
                statusMessage = $"Đi trễ {lateMinutes} phút";
            }

            var newRecord = new AttendanceRecord(
                GuidGenerator.Create(),
                userId.Value,
                today,         
                statusMessage
            );
            newRecord.CheckInTime = now;
            newRecord.LateMinutes = lateMinutes;
            newRecord.EarlyLeaveMinutes = 0;

            await _attendanceRepository.InsertAsync(newRecord);

            return $"Check-in thành công lúc {now:HH:mm} - Trạng thái: {statusMessage}";
        }

        // ==========================================
        // 3. XỬ LÝ CHECK-OUT 
        // ==========================================
        public async Task<string> CheckOutAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) 
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");

            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            var shiftEnd = today.AddHours(17);

            var record = await _attendanceRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.WorkDate == today);
            if (record == null)
                throw new UserFriendlyException("Sáng nay bạn chưa Check-in vào ca, nên không thể Check-out!");

            if (record.CheckOutTime != null)
                throw new UserFriendlyException("Hôm nay bạn đã xác nhận tan làm rồi!");

            string statusMessage = "Hoàn thành ca";
            int earlyMinutes = 0;

            if (now < shiftEnd)
            {
                earlyMinutes = (int)(shiftEnd - now).TotalMinutes;
                statusMessage = $"Về sớm {earlyMinutes} phút";
            }

            record.CheckOutTime = now;
            record.Status += $" | {statusMessage}"; 
            record.EarlyLeaveMinutes = earlyMinutes;

            await _attendanceRepository.UpdateAsync(record);

            return $"Check-out lúc {now:HH:mm}. {statusMessage} - Chúc buổi tối vui vẻ!";
        }

        // ==========================================
        // 4. API BÁO CÁO CHẤM CÔNG (ĐÃ PHÂN QUYỀN)
        // ==========================================
        public async Task<List<AttendanceReportDto>> GetDailyReportAsync(string date)
        {
            // Kiểm tra bảo mật cơ bản
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null)
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");

            DateTime parsedDate = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsed))
            {
                parsedDate = parsed.Date;
            }

            var startOfDay = parsedDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            List<AttendanceRecord> records;

            // 👉 BẢO MẬT: Kiểm tra quyền để quyết định dữ liệu trả về
            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId);
            bool isAdmin = userKey != null && (userKey.Role.ToLower() == "admin" || userKey.Role.ToLower() == "superadmin");
            
            if (isAdmin)
            {
                // Quản lý được xem toàn bộ bảng chấm công của công ty
                records = await _attendanceRepository.GetListAsync(
                    x => x.WorkDate >= startOfDay && x.WorkDate <= endOfDay
                );
            }
            else
            {
                // Nhân viên chỉ được xem đúng dòng dữ liệu của mình
                records = await _attendanceRepository.GetListAsync(
                    x => x.UserId == currentUserId && x.WorkDate >= startOfDay && x.WorkDate <= endOfDay
                );
            }

            if (!records.Any()) return new List<AttendanceReportDto>();

            var userIds = records.Select(x => x.UserId).Distinct().ToList();
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));
            var userDictionary = users.ToDictionary(x => x.Id, x => x);

            var result = new List<AttendanceReportDto>();

            foreach (var record in records)
            {
                var user = userDictionary.GetValueOrDefault(record.UserId);
                
                result.Add(new AttendanceReportDto
                {
                    EmployeeCode = user?.UserName ?? "Unknown",
                    EmployeeName = user?.Name ?? user?.UserName ?? "Không rõ",
                    CheckInTime = record.CheckInTime?.ToString("HH:mm") ?? "--:--",
                    CheckOutTime = record.CheckOutTime?.ToString("HH:mm") ?? "--:--",
                    LateMinutes = record.LateMinutes,
                    EarlyLeaveMinutes = record.EarlyLeaveMinutes
                });
            }

            return result.OrderBy(x => x.EmployeeCode).ToList();
        }
    }
}