using Microsoft.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace QuanLyNhanSu.Hubs
{
    // [ONBOARDING COMMENT]: Hub SignalR tiêu chuẩn của ABP, dùng để tạo đường ống hai chiều với Frontend.
    public class NotificationHub : AbpHub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}
