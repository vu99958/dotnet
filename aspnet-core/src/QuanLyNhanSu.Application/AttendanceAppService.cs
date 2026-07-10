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
        private readonly IRepository<Branch, Guid> _branchRepository;
        private readonly IRepository<LeaveRequest, Guid> _leaveRequestRepository;

        public AttendanceAppService(
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<UserKey, Guid> userKeyRepository,
            IRepository<Branch, Guid> branchRepository,
            IRepository<LeaveRequest, Guid> leaveRequestRepository)
        {
            _attendanceRepository = attendanceRepository;
            _userRepository = userRepository;
            _userKeyRepository = userKeyRepository;
            _branchRepository = branchRepository;
            _leaveRequestRepository = leaveRequestRepository;
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

            var groupedData = inputList.GroupBy(x => x.UserName).ToList();

            foreach (var group in groupedData)
            {
                if (!userDictionary.TryGetValue(group.Key, out Guid userId)) continue;

                var checkIn = group.Where(x => x.CheckType == "IN").OrderBy(x => x.TimeStamp).FirstOrDefault();
                var checkOut = group.Where(x => x.CheckType == "OUT").OrderByDescending(x => x.TimeStamp).FirstOrDefault();

                if (checkIn == null) continue;

                var workDate = checkIn.TimeStamp.Date;
                var shiftStart = workDate.AddHours(8);  // 08:00
                var shiftEnd = workDate.AddHours(17);   // 17:00

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
        // 2. XỬ LÝ CHECK-IN (GEOFENCING ĐA CHI NHÁNH)
        // ==========================================
        public async Task<string> CheckInAsync(double userLat, double userLng)
        {
            var userId = CurrentUser.Id;
            if (userId == null) 
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");

            // === BƯỚC 1: KIỂM TRA CHẾ ĐỘ KHẮT KHE (STRICT GEOFENCING) ===
            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == userId);
            
            if (userKey == null || !userKey.BranchId.HasValue)
                throw new UserFriendlyException("Tài khoản của bạn chưa được phân bổ về chi nhánh nào. Vui lòng liên hệ Admin!");

            var matchedBranch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == userKey.BranchId.Value);
            
            if (matchedBranch == null)
                throw new UserFriendlyException("Chi nhánh phân bổ không tồn tại hoặc đã bị xóa!");

            // Tính khoảng cách
            double dist = CalculateDistanceInMeters(userLat, userLng, matchedBranch.Latitude, matchedBranch.Longitude);
            
            if (dist > matchedBranch.RadiusInMeters)
            {
                throw new UserFriendlyException(
                    $"Chấm công thất bại: Bạn đang không ở chi nhánh làm việc đã đăng ký! (Cách {dist:F0}m)");
            }

            double minDistance = dist;

            // === BƯỚC 2: KIỂM TRA TRÙNG LẶP & NGHỈ PHÉP ===
            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            var shiftStart = today.AddHours(8);

            var approvedLeave = await _leaveRequestRepository.FirstOrDefaultAsync(x => 
                x.UserId == userId && 
                x.Status == "Approved" && 
                today >= x.StartDate.Date && 
                today <= x.EndDate.Date);

            if (approvedLeave != null)
                throw new UserFriendlyException("Hôm nay bạn đã có đơn xin nghỉ phép được phê duyệt, không cần điểm danh! Nếu bạn muốn đi làm, hãy liên hệ Admin để hủy đơn xin nghỉ.");

            var existingRecord = await _attendanceRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.WorkDate == today);
            if (existingRecord != null)
                throw new UserFriendlyException("Hôm nay bạn đã điểm danh vào ca rồi, không thể bấm 2 lần!");

            // === BƯỚC 3: TÍNH TRẠNG THÁI ĐI TRỄ ===
            string statusMessage = "Đúng giờ";
            int lateMinutes = 0;

            if (now > shiftStart)
            {
                lateMinutes = (int)(now - shiftStart).TotalMinutes;
                statusMessage = $"Đi trễ {lateMinutes} phút";
            }

            // === BƯỚC 4: LƯU BẢN GHI + TỌA ĐỘ GPS ===
            var newRecord = new AttendanceRecord(
                GuidGenerator.Create(),
                userId.Value,
                today,         
                statusMessage
            );
            newRecord.CheckInTime = now;
            newRecord.LateMinutes = lateMinutes;
            newRecord.EarlyLeaveMinutes = 0;
            newRecord.Latitude = userLat;
            newRecord.Longitude = userLng;

            await _attendanceRepository.InsertAsync(newRecord);

            return $"Check-in thành công lúc {now:HH:mm} - Trạng thái: {statusMessage} (Chi nhánh: {matchedBranch.Name}, cách {minDistance:F0}m)";
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
                statusMessage = $"Về sớm";
            }

            record.CheckOutTime = now;
            record.EarlyLeaveMinutes = earlyMinutes;

            // Xử lý hiển thị kép "Đi trễ & Về sớm"
            if (statusMessage == "Về sớm")
            {
                if (record.Status == "Đi trễ")
                    record.Status = "Đi trễ & Về sớm";
                else
                    record.Status = "Về sớm";
            }

            await _attendanceRepository.UpdateAsync(record);

            return $"Check-out lúc {now:HH:mm}. {statusMessage} - Chúc buổi tối vui vẻ!";
        }

        // ==========================================
        // 4. API BÁO CÁO CHẤM CÔNG NGÀY (ĐÃ PHÂN QUYỀN + PHÁT HIỆN VẮNG MẶT)
        // ==========================================
        // 🔑 LOGIC MỚI: Lấy danh sách TẤT CẢ nhân viên làm gốc (Left Join),
        //    thay vì chỉ lấy từ bảng AttendanceRecord.
        //    → Ai không có dữ liệu check-in VÀ không có đơn nghỉ phép = "Vắng mặt không phép".
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

            // 👉 BẢO MẬT: Kiểm tra quyền để quyết định dữ liệu trả về
            var currentUserKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId);
            bool isAdmin = currentUserKey != null && (currentUserKey.Role.ToLower() == "admin" || currentUserKey.Role.ToLower() == "superadmin");

            // ==========================================
            // BƯỚC 1: Lấy danh sách nhân viên làm GỐC
            // ==========================================
            List<IdentityUser> allUsers;
            if (isAdmin)
            {
                // Admin: xem tất cả nhân viên trong hệ thống
                allUsers = await _userRepository.GetListAsync();
            }
            else
            {
                // Nhân viên thường: chỉ xem chính mình
                var me = await _userRepository.FirstOrDefaultAsync(x => x.Id == currentUserId);
                allUsers = me != null ? new List<IdentityUser> { me } : new List<IdentityUser>();
            }

            if (!allUsers.Any()) return new List<AttendanceReportDto>();

            // ==========================================
            // BƯỚC 2: Lấy toàn bộ dữ liệu liên quan trong ngày
            // ==========================================
            var allUserIds = allUsers.Select(u => u.Id).ToList();

            // Lấy tất cả bản ghi chấm công trong ngày
            var attendanceRecords = await _attendanceRepository.GetListAsync(
                x => allUserIds.Contains(x.UserId) && x.WorkDate == parsedDate
            );

            // Lấy tất cả đơn nghỉ phép được duyệt mà ngày `parsedDate` nằm trong khoảng
            var approvedLeaves = await _leaveRequestRepository.GetListAsync(
                x => allUserIds.Contains(x.UserId)
                     && x.Status == "Approved"
                     && parsedDate >= x.StartDate.Date
                     && parsedDate <= x.EndDate.Date
            );

            // Lấy thông tin UserKey (chi nhánh, vai trò)
            var userKeys = await _userKeyRepository.GetListAsync(x => allUserIds.Contains(x.UserId));
            var userKeyDict = userKeys.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First());

            // Lấy danh sách chi nhánh
            var branches = await _branchRepository.GetListAsync();
            var branchDict = branches.ToDictionary(b => b.Id, b => b.Name);

            // ==========================================
            // BƯỚC 3: LEFT JOIN thủ công trong C# (In-Memory)
            // Cú pháp: Duyệt từng User, tìm AttendanceRecord & LeaveRequest tương ứng.
            // Nếu KHÔNG TÌM THẤY → đó chính là Left Join trả null.
            // ==========================================
            var result = new List<AttendanceReportDto>();

            foreach (var user in allUsers)
            {
                // Left Join: Tìm bản ghi chấm công của user trong ngày
                var record = attendanceRecords.FirstOrDefault(r => r.UserId == user.Id);

                // Left Join: Tìm đơn xin nghỉ phép được duyệt cho user trong ngày
                var leave = approvedLeaves.FirstOrDefault(l => l.UserId == user.Id);

                // Lấy thông tin chi nhánh
                var key = userKeyDict.GetValueOrDefault(user.Id);
                string branchName = "Không xác định";
                if (key?.BranchId != null && branchDict.ContainsKey(key.BranchId.Value))
                {
                    branchName = branchDict[key.BranchId.Value];
                }

                // ==========================================
                // BƯỚC 4: XÁC ĐỊNH TRẠNG THÁI TỔNG HỢP
                // ==========================================
                var dto = new AttendanceReportDto
                {
                    EmployeeCode = user.UserName ?? "Unknown",
                    EmployeeName = user.Name ?? user.UserName ?? "Không rõ",
                    BranchName = branchName
                };

                if (record != null)
                {
                    // ✅ CÓ dữ liệu chấm công → "Có mặt"
                    dto.CheckInTime = record.CheckInTime?.ToString("HH:mm") ?? "--:--";
                    dto.CheckOutTime = record.CheckOutTime?.ToString("HH:mm") ?? "--:--";
                    dto.LateMinutes = record.LateMinutes;
                    dto.EarlyLeaveMinutes = record.EarlyLeaveMinutes;
                    dto.Latitude = record.Latitude;
                    dto.Longitude = record.Longitude;
                    dto.AttendanceStatus = "Có mặt";
                }
                else if (leave != null)
                {
                    // 📋 KHÔNG có chấm công, NHƯNG CÓ đơn nghỉ phép → "Nghỉ có phép"
                    dto.CheckInTime = "--:--";
                    dto.CheckOutTime = "--:--";
                    dto.LateMinutes = 0;
                    dto.EarlyLeaveMinutes = 0;
                    dto.AttendanceStatus = "Nghỉ có phép";
                }
                else
                {
                    // ❌ KHÔNG có chấm công VÀ KHÔNG có đơn nghỉ → "Vắng mặt không phép"
                    dto.CheckInTime = "--:--";
                    dto.CheckOutTime = "--:--";
                    dto.LateMinutes = 0;
                    dto.EarlyLeaveMinutes = 0;
                    dto.AttendanceStatus = "Vắng mặt không phép";
                }

                result.Add(dto);
            }

            return result.OrderBy(x => x.EmployeeCode).ToList();
        }

        // ==========================================
        // HAVERSINE FORMULA - TÍNH KHOẢNG CÁCH (MÉT)
        // ==========================================
        private double CalculateDistanceInMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Bán kính Trái Đất (mét)
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // ==========================================
        // 5. API HỦY BỎ CHẤM CÔNG (CHỈ ADMIN)
        // ==========================================
        [Authorize]
        public async Task DeleteDailyAttendanceAsync(string userName, string date)
        {
            // 1. Kiểm tra bảo mật
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null)
                throw new UserFriendlyException("Bạn chưa đăng nhập!");

            var userKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId);
            bool isAdmin = userKey != null && (userKey.Role.ToLower() == "admin" || userKey.Role.ToLower() == "superadmin");
            
            if (!isAdmin)
                throw new UserFriendlyException("Chỉ có Admin hoặc SuperAdmin mới có quyền hủy chấm công!");

            // 2. Parse ngày tháng
            DateTime parsedDate = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsed))
            {
                parsedDate = parsed.Date;
            }

            // 3. Tìm nhân viên
            var targetUser = await _userRepository.FirstOrDefaultAsync(x => x.UserName == userName);
            if (targetUser == null)
                throw new UserFriendlyException($"Không tìm thấy nhân viên có mã {userName}");

            // 4. Tìm và xóa bản ghi
            var record = await _attendanceRepository.FirstOrDefaultAsync(x => x.UserId == targetUser.Id && x.WorkDate == parsedDate);
            if (record == null)
                throw new UserFriendlyException($"Nhân viên {userName} không có dữ liệu chấm công trong ngày {parsedDate:dd/MM/yyyy}");

            await _attendanceRepository.DeleteAsync(record);
        }

        // ==========================================
        // 6. API BÁO CÁO TỔNG HỢP THEO THÁNG (CHỐT LƯƠNG)
        // ==========================================
        // 🔑 LOGIC: Duyệt từng ngày trong khoảng [fromDate → toDate], bỏ qua T7/CN.
        //    Mỗi ngày thực hiện Left Join tương tự GetDailyReportAsync:
        //    - Có check-in → cộng TotalWorkDays
        //    - Có check-in nhưng KHÔNG có check-out → cộng TotalMissingCheckOuts (Nhiệm vụ 4)
        //    - Không có check-in, có đơn nghỉ → cộng TotalLeaveDays
        //    - Không có gì cả → cộng TotalAbsentDays
        // ==========================================
        [Authorize]
        public async Task<List<MonthlyAttendanceDto>> GetMonthlyReportAsync(string fromDate, string toDate)
        {
            // Kiểm tra bảo mật
            var currentUserId = CurrentUser.Id;
            if (currentUserId == null)
                throw new UserFriendlyException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn!");

            // Chỉ Admin/SuperAdmin mới được xem báo cáo tháng (vì đây là báo cáo tổng hợp chốt lương)
            var currentUserKey = await _userKeyRepository.FirstOrDefaultAsync(k => k.UserId == currentUserId);
            bool isAdmin = currentUserKey != null && (currentUserKey.Role.ToLower() == "admin" || currentUserKey.Role.ToLower() == "superadmin");
            if (!isAdmin)
                throw new UserFriendlyException("Chỉ có Admin hoặc SuperAdmin mới có quyền xem báo cáo tổng hợp tháng!");

            // Parse ngày
            DateTime parsedFrom = DateTime.Now.Date.AddDays(-30);
            DateTime parsedTo = DateTime.Now.Date;

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var f))
                parsedFrom = f.Date;
            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var t))
                parsedTo = t.Date;

            // ==========================================
            // BƯỚC 1: Lấy danh sách tất cả nhân viên
            // ==========================================
            var allUsers = await _userRepository.GetListAsync();
            if (!allUsers.Any()) return new List<MonthlyAttendanceDto>();

            var allUserIds = allUsers.Select(u => u.Id).ToList();

            // ==========================================
            // BƯỚC 2: Lấy toàn bộ dữ liệu chấm công trong khoảng thời gian
            // ==========================================
            var allRecords = await _attendanceRepository.GetListAsync(
                x => allUserIds.Contains(x.UserId) && x.WorkDate >= parsedFrom && x.WorkDate <= parsedTo
            );

            // Lấy tất cả đơn nghỉ phép đã duyệt có giao nhau với khoảng thời gian
            var allLeaves = await _leaveRequestRepository.GetListAsync(
                x => allUserIds.Contains(x.UserId)
                     && x.Status == "Approved"
                     && x.StartDate.Date <= parsedTo
                     && x.EndDate.Date >= parsedFrom
            );

            // Lấy UserKey (chi nhánh)
            var userKeys = await _userKeyRepository.GetListAsync(x => allUserIds.Contains(x.UserId));
            var userKeyDict = userKeys.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First());

            var branches = await _branchRepository.GetListAsync();
            var branchDict = branches.ToDictionary(b => b.Id, b => b.Name);

            // ==========================================
            // BƯỚC 3: Tính danh sách ngày làm việc (loại T7, CN)
            // ==========================================
            var workingDays = new List<DateTime>();
            for (var d = parsedFrom; d <= parsedTo; d = d.AddDays(1))
            {
                // Bỏ qua Thứ 7 (Saturday) và Chủ nhật (Sunday)
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays.Add(d);
                }
            }

            // ==========================================
            // BƯỚC 4: GOM NHÓM THEO TỪNG NHÂN VIÊN (GroupBy UserId)
            //    Duyệt từng ngày làm việc → Left Join → Cộng dồn
            // ==========================================
            var result = new List<MonthlyAttendanceDto>();

            foreach (var user in allUsers)
            {
                // Lọc riêng dữ liệu chấm công + nghỉ phép của nhân viên này
                var userRecords = allRecords.Where(r => r.UserId == user.Id).ToList();
                var userLeaves = allLeaves.Where(l => l.UserId == user.Id).ToList();

                // Lấy chi nhánh
                var key = userKeyDict.GetValueOrDefault(user.Id);
                string branchName = "Không xác định";
                if (key?.BranchId != null && branchDict.ContainsKey(key.BranchId.Value))
                {
                    branchName = branchDict[key.BranchId.Value];
                }

                var dto = new MonthlyAttendanceDto
                {
                    UserId = user.Id,
                    FullName = user.Name ?? user.UserName ?? "Không rõ",
                    BranchName = branchName
                };

                // Duyệt từng ngày làm việc trong khoảng thời gian
                foreach (var day in workingDays)
                {
                    // Left Join: tìm bản ghi chấm công ngày này
                    var record = userRecords.FirstOrDefault(r => r.WorkDate == day);

                    // Left Join: tìm đơn nghỉ phép cho ngày này
                    var leave = userLeaves.FirstOrDefault(l => day >= l.StartDate.Date && day <= l.EndDate.Date);

                    if (record != null)
                    {
                        // ✅ CÓ check-in → cộng 1 ngày công
                        dto.TotalWorkDays++;
                        dto.TotalLateMinutes += record.LateMinutes;
                        dto.TotalEarlyLeaveMinutes += record.EarlyLeaveMinutes;

                        // 🔎 NHIỆM VỤ 4: Phát hiện quên Check-out
                        // Nếu có CheckInTime nhưng CheckOutTime là null → quên check-out
                        if (record.CheckInTime != null && record.CheckOutTime == null)
                        {
                            dto.TotalMissingCheckOuts++;
                        }
                    }
                    else if (leave != null)
                    {
                        // 📋 Nghỉ có phép
                        dto.TotalLeaveDays++;
                    }
                    else
                    {
                        // ❌ Vắng mặt không phép (không check-in, không có đơn nghỉ)
                        dto.TotalAbsentDays++;
                    }
                }

                result.Add(dto);
            }

            return result.OrderBy(x => x.FullName).ToList();
        }
    }
}