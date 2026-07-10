using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Interface dịch vụ quản lý dữ liệu sinh trắc học (DDD Application Layer).
    /// 
    /// Luồng nghiệp vụ chính:
    /// 1. Desktop Client đọc vân tay/khuôn mặt từ Máy A → Gọi UploadTemplatesAsync() để lưu lên Server.
    /// 2. Desktop Client gọi GetAllTemplatesAsync() để tải toàn bộ mẫu từ Server.
    /// 3. Desktop Client ghi mẫu xuống Máy B bằng SDK ZKTeco.
    /// </summary>
    public interface IBiometricAppService : IApplicationService
    {
        /// <summary>
        /// Upload (Lưu) danh sách mẫu sinh trắc học từ Desktop Client lên Server.
        /// Logic: Nếu đã tồn tại (EnrollNumber + TemplateType + FingerIndex) → cập nhật.
        ///        Nếu chưa tồn tại → thêm mới.
        /// </summary>
        /// <param name="templates">Danh sách mẫu cần lưu</param>
        /// <returns>Số lượng mẫu đã lưu thành công</returns>
        Task<int> UploadTemplatesAsync(List<BiometricTemplateDto> templates);

        /// <summary>
        /// Lấy tất cả mẫu sinh trắc học của một nhân viên cụ thể.
        /// Dùng khi cần kiểm tra nhân viên đã có bao nhiêu vân tay, có khuôn mặt chưa.
        /// </summary>
        Task<List<BiometricTemplateDto>> GetTemplatesByEnrollNumberAsync(string enrollNumber);

        /// <summary>
        /// Lấy toàn bộ mẫu sinh trắc học trên Server.
        /// Dùng khi cần đồng bộ xuống một máy chấm công mới (máy trống).
        /// </summary>
        Task<List<BiometricTemplateDto>> GetAllTemplatesAsync();

        /// <summary>
        /// Xóa tất cả mẫu sinh trắc học của một nhân viên.
        /// Dùng khi nhân viên nghỉ việc hoặc cần đăng ký lại.
        /// </summary>
        Task DeleteTemplatesByEnrollNumberAsync(string enrollNumber);
    }
}
