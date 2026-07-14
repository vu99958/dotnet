using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    public interface IPayslipAppService : IApplicationService
    {
        Task<string> GenerateMonthlyPayrollAsync(int month, int year);
        Task<List<PayslipDto>> GetListAsync(int month, int year);
    }
}
