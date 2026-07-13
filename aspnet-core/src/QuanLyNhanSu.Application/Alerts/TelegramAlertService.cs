using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace QuanLyNhanSu.Alerts
{
    // [ONBOARDING COMMENT]: Service thực hiện gọi API ra bên ngoài (Telegram).
    // Được dùng trong Background Worker để gửi cảnh báo thiết bị Offline.
    public class TelegramAlertService : ITelegramAlertService, ITransientDependency
    {
        private readonly ILogger<TelegramAlertService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public TelegramAlertService(ILogger<TelegramAlertService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task SendAlertAsync(string message)
        {
            var botToken = _configuration["Telegram:BotToken"];
            var chatId = _configuration["Telegram:ChatId"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                // Fallback: Sếp chưa điền Token, chỉ log ra Console
                _logger.LogWarning($"[TELEGRAM ALERT FALLBACK] (Missing Token): {message}");
                return;
            }

            try
            {
                var url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to send Telegram alert. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending Telegram alert.");
            }
        }
    }
}
