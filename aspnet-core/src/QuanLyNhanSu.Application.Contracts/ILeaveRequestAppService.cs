using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    public interface ILeaveRequestAppService :
        ICrudAppService<
            LeaveRequestDto, //Used to show leave requests
            Guid, //Primary key of the leave request entity
            PagedAndSortedResultRequestDto, //Used for paging/sorting
            CreateUpdateLeaveRequestDto> //Used to create/update a leave request
    {
        // Hàm dành cho Admin để duyệt/từ chối đơn
        Task ChangeStatusAsync(Guid id, string newStatus);
    }
}
