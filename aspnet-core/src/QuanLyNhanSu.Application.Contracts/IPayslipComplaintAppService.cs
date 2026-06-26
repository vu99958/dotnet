using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    public interface IPayslipComplaintAppService : IApplicationService
    {
        Task<List<PayslipComplaintDto>> GetMyComplaintsAsync();
        Task<List<PayslipComplaintDto>> GetPendingListAsync(); // For Admin
        Task CreateAsync(CreateComplaintDto input);
        Task ResolveAsync(Guid id, ResolveComplaintDto input); // For Admin
    }
}
