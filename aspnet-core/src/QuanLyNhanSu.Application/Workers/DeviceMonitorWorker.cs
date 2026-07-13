using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using QuanLyNhanSu.Alerts;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain;
using System.Collections.Generic;

namespace QuanLyNhanSu.Workers
{
    // [ONBOARDING COMMENT]: Background Worker chạy ngầm định kỳ 15 phút 1 lần.
    // Kiểm tra tất cả thiết bị/chi nhánh xem có thiết bị nào rớt mạng không.
    public class DeviceMonitorWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public DeviceMonitorWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory) 
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = 15 * 60 * 1000; // 15 phút
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<DeviceMonitorWorker>>();
            var cache = workerContext.ServiceProvider.GetRequiredService<IDistributedCache>();
            var alertService = workerContext.ServiceProvider.GetRequiredService<ITelegramAlertService>();
            var branchRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Branch, Guid>>();

            logger.LogInformation("DeviceMonitorWorker is checking device statuses...");

            var branches = await branchRepository.GetListAsync();
            var offlineDevices = new List<string>();

            foreach (var branch in branches)
            {
                // Giả định SN thiết bị trùng với mã chi nhánh hoặc lấy từ cấu hình. Tạm dùng BranchName làm SN.
                var deviceSn = branch.Name; 
                var cacheKey = $"DevicePing_{deviceSn}";
                var lastPingStr = await cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(lastPingStr))
                {
                    offlineDevices.Add(deviceSn);
                    continue;
                }

                if (DateTime.TryParse(lastPingStr, out DateTime lastPing))
                {
                    if ((DateTime.UtcNow - lastPing).TotalMinutes > 15)
                    {
                        offlineDevices.Add(deviceSn);
                    }
                }
                else
                {
                    offlineDevices.Add(deviceSn);
                }
            }

            if (offlineDevices.Count > 0)
            {
                string alertMsg = $"🚨 [BÁO ĐỘNG HỆ THỐNG]\nPhát hiện {offlineDevices.Count} thiết bị chấm công MẤT KẾT NỐI quá 15 phút!\nChi nhánh: {string.Join(", ", offlineDevices)}";
                logger.LogWarning(alertMsg);
                await alertService.SendAlertAsync(alertMsg);
            }
        }
    }
}
