using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Events;
using QuanLyNhanSu.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace QuanLyNhanSu.Handlers
{
    // [ONBOARDING COMMENT]: Handler này chạy ngầm (Background) khi có BulkSyncRequestedEvent.
    // Giúp API Http không bị block (timeout) khi đẩy hàng chục nghìn record cuối tháng.
    public class BulkSyncEventHandler : ILocalEventHandler<BulkSyncRequestedEvent>, ITransientDependency
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly AttendanceManager _attendanceManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<BulkSyncEventHandler> _logger;

        public BulkSyncEventHandler(
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            AttendanceManager attendanceManager,
            IGuidGenerator guidGenerator,
            ILogger<BulkSyncEventHandler> logger)
        {
            _userRepository = userRepository;
            _attendanceRepository = attendanceRepository;
            _attendanceManager = attendanceManager;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        [UnitOfWork]
        public virtual async Task HandleEventAsync(BulkSyncRequestedEvent eventData)
        {
            var inputList = eventData.AttendanceData;
            if (inputList == null || !inputList.Any()) return;

            _logger.LogInformation($"[BulkSyncEvent] Started processing {inputList.Count} attendance records.");

            try
            {
                // BƯỚC 1: Lấy danh sách User (Batch Query)
                var userNames = inputList.Select(x => x.UserName).Distinct().ToList();
                var users = await _userRepository.GetListAsync(x => userNames.Contains(x.UserName));
                var userDictionary = users.ToDictionary(x => x.UserName, x => x.Id);

                // Gom nhóm theo (UserName, WorkDate)
                var groupedData = inputList.GroupBy(x => new { x.UserName, WorkDate = x.TimeStamp.Date }).ToList();

                // Lấy danh sách các UserId và WorkDate cần xử lý
                var userIdsToSync = userDictionary.Values.ToList();
                var datesToSync = groupedData.Select(g => g.Key.WorkDate).Distinct().ToList();

                // [ONBOARDING COMMENT]: BƯỚC 2: ANTI N+1 QUERY. Lấy toàn bộ AttendanceRecord của các User trong các ngày này lên RAM.
                var existingRecords = await _attendanceRepository.GetListAsync(
                    x => userIdsToSync.Contains(x.UserId) && datesToSync.Contains(x.WorkDate)
                );

                var existingRecordsDict = existingRecords
                    .GroupBy(x => new { x.UserId, x.WorkDate })
                    .ToDictionary(g => g.Key, g => g.First());

                var listToSave = new List<AttendanceRecord>();
                var listToUpdate = new List<AttendanceRecord>();

                foreach (var group in groupedData)
                {
                    if (!userDictionary.TryGetValue(group.Key.UserName, out Guid userId)) continue;

                    var checkIn = group.Where(x => x.CheckType == "IN" || x.CheckType == "0").OrderBy(x => x.TimeStamp).FirstOrDefault();
                    var checkOut = group.Where(x => x.CheckType == "OUT" || x.CheckType == "1").OrderByDescending(x => x.TimeStamp).FirstOrDefault();

                    if (checkIn == null && checkOut == null) continue;

                    var workDate = group.Key.WorkDate;

                    // Sử dụng Dictionary thay vì gọi DB FirstOrDefaultAsync (tránh N+1)
                    existingRecordsDict.TryGetValue(new { UserId = userId, WorkDate = workDate }, out var existingRecord);

                    // TODO: Cấu hình linh hoạt theo Chi nhánh
                    var shiftStart = workDate.AddHours(8);  // 08:00
                    var shiftEnd = workDate.AddHours(17);   // 17:00

                    var finalCheckIn = checkIn?.TimeStamp ?? existingRecord?.CheckInTime;
                    var finalCheckOut = checkOut?.TimeStamp ?? existingRecord?.CheckOutTime;

                    var (lateMinutes, earlyMinutes) = _attendanceManager.CalculateLateAndEarly(finalCheckIn, finalCheckOut, shiftStart, shiftEnd);

                    if (existingRecord != null)
                    {
                        existingRecord.CheckInTime = finalCheckIn;
                        existingRecord.CheckOutTime = finalCheckOut;
                        existingRecord.LateMinutes = lateMinutes;
                        existingRecord.EarlyLeaveMinutes = earlyMinutes;
                        
                        if (!listToUpdate.Contains(existingRecord))
                            listToUpdate.Add(existingRecord);
                    }
                    else
                    {
                        var record = new AttendanceRecord(
                            _guidGenerator.Create(),
                            userId,
                            workDate,
                            "Hệ thống tự động đồng bộ (Event)"
                        )
                        {
                            CheckInTime = finalCheckIn,
                            CheckOutTime = finalCheckOut,
                            LateMinutes = lateMinutes,
                            EarlyLeaveMinutes = earlyMinutes
                        };
                        listToSave.Add(record);
                        
                        // Thêm vào dict để xử lý trùng trong cùng 1 cục bulk
                        existingRecordsDict.Add(new { UserId = userId, WorkDate = workDate }, record);
                    }
                }

                // Bulk thao tác DB
                if (listToSave.Any())
                    await _attendanceRepository.InsertManyAsync(listToSave);

                if (listToUpdate.Any())
                    await _attendanceRepository.UpdateManyAsync(listToUpdate);

                _logger.LogInformation($"[BulkSyncEvent] Completed. Inserted {listToSave.Count}, Updated {listToUpdate.Count}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BulkSyncEvent] Failed to process bulk sync event.");
            }
        }
    }
}
