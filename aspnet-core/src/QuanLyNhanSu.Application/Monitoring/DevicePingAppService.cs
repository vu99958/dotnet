using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;

namespace QuanLyNhanSu.Monitoring
{
    // [ONBOARDING COMMENT]: API này được gọi định kỳ bởi WinForms Client hoặc thiết bị ADMS
    // Để báo hiệu thiết bị vẫn đang "sống".
    // Cấm dùng [Authorize] nếu ADMS gọi trực tiếp (vì không có Token).
    [AllowAnonymous]
    public class DevicePingAppService : QuanLyNhanSuAppService, IDevicePingAppService
    {
        private readonly IDistributedCache<string> _cache;

        public DevicePingAppService(IDistributedCache<string> cache)
        {
            _cache = cache;
        }

        public async Task PingAsync(string deviceSn)
        {
            if (string.IsNullOrEmpty(deviceSn)) return;

            // [ONBOARDING COMMENT]: Lưu LastPingTime vào Redis/MemoryCache thay vì Database
            // Tránh write DB liên tục gây thắt cổ chai
            var cacheKey = $"DevicePing_{deviceSn}";
            await _cache.SetAsync(
                cacheKey,
                DateTime.UtcNow.ToString("o"), // ISO 8601 format
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // Xóa sau 1 giờ nếu không có tín hiệu
                }
            );
        }
    }
}
