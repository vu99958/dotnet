using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu.Monitoring
{
    public interface IDevicePingAppService : IApplicationService
    {
        Task PingAsync(string deviceSn);
    }
}
