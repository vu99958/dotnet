using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Hubs;
using QuanLyNhanSu.Services;

namespace QuanLyNhanSu.Attendance
{
    public class AttendanceDistributedEventHandler : IDistributedEventHandler<AttendancePushedEto>, ITransientDependency
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly AttendanceManager _attendanceManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<AttendanceDistributedEventHandler> _logger;

        public AttendanceDistributedEventHandler(
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            AttendanceManager attendanceManager,
            IGuidGenerator guidGenerator,
            IHubContext<NotificationHub> hubContext,
            ILogger<AttendanceDistributedEventHandler> logger)
        {
            _userRepository = userRepository;
            _attendanceRepository = attendanceRepository;
            _attendanceManager = attendanceManager;
            _guidGenerator = guidGenerator;
            _hubContext = hubContext;
            _logger = logger;
        }

        [UnitOfWork]
        public async Task HandleEventAsync(AttendancePushedEto eventData)
        {
            var inputList = eventData.Logs;
            if (inputList == null || !inputList.Any()) return;

            _logger.LogInformation($"[RabbitMQ - Attendance Sync] Started processing {inputList.Count} attendance records.");

            try
            {
                // BƯỚC 1: Lấy danh sách User (Batch Query)
                var userNames = inputList.Select(x => x.UserName).Distinct().ToList();
                var users = await _userRepository.GetListAsync(x => userNames.Contains(x.UserName));
                var userDictionary = users.ToDictionary(x => x.UserName, x => x.Id);

                // Gom nhóm theo (UserName, WorkDate)
                var groupedData = inputList.GroupBy(x => new { x.UserName, WorkDate = x.TimeStamp.Date }).ToList();

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

                    existingRecordsDict.TryGetValue(new { UserId = userId, WorkDate = workDate }, out var existingRecord);

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
                            "Hệ thống tự động đồng bộ (RabbitMQ)"
                        )
                        {
                            CheckInTime = finalCheckIn,
                            CheckOutTime = finalCheckOut,
                            LateMinutes = lateMinutes,
                            EarlyLeaveMinutes = earlyMinutes
                        };
                        listToSave.Add(record);
                        
                        existingRecordsDict.Add(new { UserId = userId, WorkDate = workDate }, record);
                    }
                }

                if (listToSave.Any())
                    await _attendanceRepository.InsertManyAsync(listToSave);

                if (listToUpdate.Any())
                    await _attendanceRepository.UpdateManyAsync(listToUpdate);

                // [ONBOARDING COMMENT]: Bắn thông báo SignalR Real-time về Web Dashboard
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", 
                    $"Đã đồng bộ thành công {listToSave.Count} bản ghi mới và cập nhật {listToUpdate.Count} bản ghi từ {eventData.BranchName}");
                    
                _logger.LogInformation($"[RabbitMQ - Attendance Sync] Completed. Inserted {listToSave.Count}, Updated {listToUpdate.Count}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RabbitMQ - Attendance Sync] Failed to process event.");
                throw; // Throw to trigger RabbitMQ DLQ/Retry mechanism
            }
        }
    }
}
