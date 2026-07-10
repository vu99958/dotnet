using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Service quản lý dữ liệu sinh trắc học — CRUD đầy đủ (DDD Application Layer).
    /// 
    /// Nhiệm vụ: Nhận dữ liệu vân tay/khuôn mặt từ Desktop Client, lưu vào DB,
    /// và phục vụ việc đồng bộ chéo giữa các máy chấm công.
    /// </summary>
    public class BiometricAppService : QuanLyNhanSuAppService, IBiometricAppService
    {
        private readonly IRepository<BiometricTemplate, Guid> _biometricRepository;

        public BiometricAppService(IRepository<BiometricTemplate, Guid> biometricRepository)
        {
            _biometricRepository = biometricRepository;
        }

        // ==========================================
        // 1. UPLOAD MẪU SINH TRẮC HỌC TỪ CLIENT
        // ==========================================
        /// <summary>
        /// Lưu danh sách mẫu sinh trắc học lên Server.
        /// Logic chống trùng: Nếu đã có (EnrollNumber + TemplateType + FingerIndex) → Cập nhật dữ liệu mới.
        /// </summary>
        [AllowAnonymous]
        public async Task<int> UploadTemplatesAsync(List<BiometricTemplateDto> templates)
        {
            if (templates == null || !templates.Any()) return 0;

            int savedCount = 0;

            foreach (var dto in templates)
            {
                // Tìm bản ghi hiện có (dựa trên 3 khóa: EnrollNumber + TemplateType + FingerIndex)
                var existing = await _biometricRepository.FirstOrDefaultAsync(x =>
                    x.EnrollNumber == dto.EnrollNumber
                    && x.TemplateType == dto.TemplateType
                    && x.FingerIndex == dto.FingerIndex
                );

                if (existing != null)
                {
                    // Đã tồn tại → Cập nhật dữ liệu template mới
                    existing.TemplateData = dto.TemplateData;
                    existing.TemplateLength = dto.TemplateLength;
                    existing.SourceDeviceSerial = dto.SourceDeviceSerial;
                    existing.RegisteredAt = DateTime.Now;

                    await _biometricRepository.UpdateAsync(existing);
                }
                else
                {
                    // Chưa có → Thêm mới
                    var entity = new BiometricTemplate(
                        GuidGenerator.Create(),
                        dto.EnrollNumber,
                        dto.TemplateType,
                        dto.FingerIndex,
                        dto.TemplateData,
                        dto.TemplateLength,
                        dto.SourceDeviceSerial,
                        dto.UserId
                    );

                    await _biometricRepository.InsertAsync(entity);
                }

                savedCount++;
            }

            return savedCount;
        }

        // ==========================================
        // 2. LẤY MẪU THEO MÃ NHÂN VIÊN
        // ==========================================
        [AllowAnonymous]
        public async Task<List<BiometricTemplateDto>> GetTemplatesByEnrollNumberAsync(string enrollNumber)
        {
            var list = await _biometricRepository.GetListAsync(x => x.EnrollNumber == enrollNumber);

            return list.Select(MapToDto).OrderBy(x => x.TemplateType).ThenBy(x => x.FingerIndex).ToList();
        }

        // ==========================================
        // 3. LẤY TOÀN BỘ MẪU (ĐỂ ĐỒNG BỘ XUỐNG MÁY MỚI)
        // ==========================================
        [AllowAnonymous]
        public async Task<List<BiometricTemplateDto>> GetAllTemplatesAsync()
        {
            var list = await _biometricRepository.GetListAsync();

            return list.Select(MapToDto).OrderBy(x => x.EnrollNumber).ThenBy(x => x.TemplateType).ThenBy(x => x.FingerIndex).ToList();
        }

        // ==========================================
        // 4. XÓA MẪU THEO MÃ NHÂN VIÊN
        // ==========================================
        [Authorize]
        public async Task DeleteTemplatesByEnrollNumberAsync(string enrollNumber)
        {
            await _biometricRepository.DeleteAsync(x => x.EnrollNumber == enrollNumber);
        }

        // ==========================================
        // HELPER: MAP ENTITY → DTO
        // ==========================================
        private BiometricTemplateDto MapToDto(BiometricTemplate entity)
        {
            return new BiometricTemplateDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                EnrollNumber = entity.EnrollNumber,
                TemplateType = entity.TemplateType,
                FingerIndex = entity.FingerIndex,
                TemplateData = entity.TemplateData,
                TemplateLength = entity.TemplateLength,
                SourceDeviceSerial = entity.SourceDeviceSerial,
                RegisteredAt = entity.RegisteredAt
            };
        }
    }
}
