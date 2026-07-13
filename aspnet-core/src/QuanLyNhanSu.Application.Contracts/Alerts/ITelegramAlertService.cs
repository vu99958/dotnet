using System.Threading.Tasks;

namespace QuanLyNhanSu.Alerts
{
    public interface ITelegramAlertService
    {
        Task SendAlertAsync(string message);
    }
}
