using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NSubstitute;
using QuanLyNhanSu.Domain;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;
using QuanLyNhanSu.Enums;

namespace QuanLyNhanSu
{
    /// <summary>
    /// Unit Test kiểm chứng tính năng Đồng bộ Sinh trắc học bằng dữ liệu Base64 thật
    /// mô phỏng định dạng của thiết bị Ronald Jack / ZKTeco.
    /// </summary>
    public class BiometricAppServiceTests
    {
        private readonly BiometricAppService _biometricAppService;
        private readonly IRepository<BiometricTemplate, Guid> _biometricRepository;
        private readonly IGuidGenerator _guidGenerator;

        // Dữ liệu giả lập mô phỏng chuỗi Base64 thật được trích xuất từ thiết bị Ronald Jack
        private const string REAL_ZKT_FINGER_BASE64 = "T1BMTwUAAAAyAAAARwAAAE0AAABPAAAAUQAAAFMAAABVAAAAWAAAAFsAAABdAAAAXwAAAGEAAABkAAAAZwAAAGkAAABrAAAAbQAAAG8AAABxAAAAcwAAAHUAAAB3AAAAeQAAAHsAAAB9AAAAfwAAAIEAAACDAAAAhQAAAIcAAACJAAAAiwAAAI0AAACPAAAA";
        private const string REAL_ZKT_FACE_BASE64 = "MTIzNDU2Nzg5MGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY3ODkwYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXpBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWg==";

        public BiometricAppServiceTests()
        {
            _biometricRepository = Substitute.For<IRepository<BiometricTemplate, Guid>>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(Guid.NewGuid());

            var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
            lazyServiceProvider.LazyGetService<IGuidGenerator>().Returns(_guidGenerator);

            _biometricAppService = new BiometricAppService(_biometricRepository)
            {
                LazyServiceProvider = lazyServiceProvider
            };
        }

        [Fact]
        public async Task UploadTemplatesAsync_Should_Insert_New_Real_Templates()
        {
            // Arrange
            var inputList = new List<BiometricTemplateDto>
            {
                new BiometricTemplateDto
                {
                    EnrollNumber = "100",
                    TemplateType = BiometricType.Fingerprint,
                    FingerIndex = 1,
                    TemplateData = REAL_ZKT_FINGER_BASE64
                },
                new BiometricTemplateDto
                {
                    EnrollNumber = "100",
                    TemplateType = BiometricType.Face,
                    FingerIndex = 50,
                    TemplateData = REAL_ZKT_FACE_BASE64
                }
            };

            // Setup mock: trả về null cho firstordefault (nghĩa là chưa có template nào trong db)
            _biometricRepository.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<BiometricTemplate, bool>>>(),
                Arg.Any<System.Threading.CancellationToken>()
            ).Returns(Task.FromResult<BiometricTemplate>(null!));

            // Setup mock Insert để track xem nó có insert đúng không
            var insertedTemplates = new List<BiometricTemplate>();
            _biometricRepository.InsertAsync(Arg.Do<BiometricTemplate>(t => insertedTemplates.Add(t))).Returns(callInfo => Task.FromResult(callInfo.Arg<BiometricTemplate>()));

            // Act
            int resultCount = await _biometricAppService.UploadTemplatesAsync(inputList);

            // Assert
            resultCount.ShouldBe(2);
            insertedTemplates.Count.ShouldBe(2);
            
            var finger = insertedTemplates.FirstOrDefault(x => x.TemplateType == BiometricType.Fingerprint);
            finger.ShouldNotBeNull();
            finger.TemplateData.ShouldBe(REAL_ZKT_FINGER_BASE64);

            var face = insertedTemplates.FirstOrDefault(x => x.TemplateType == BiometricType.Face);
            face.ShouldNotBeNull();
            face.TemplateData.ShouldBe(REAL_ZKT_FACE_BASE64);
        }

        [Fact]
        public async Task UploadTemplatesAsync_Should_Update_Existing_Templates_Without_Duplicating()
        {
            // Arrange
            var existingTemplate = new BiometricTemplate(Guid.NewGuid(), "200", BiometricType.Fingerprint, 0, "OLD_DATA", "OLD_DATA".Length);

            var updatedDto = new BiometricTemplateDto
            {
                EnrollNumber = "200",
                TemplateType = BiometricType.Fingerprint,
                FingerIndex = 0,
                TemplateData = REAL_ZKT_FINGER_BASE64
            };

            // Setup mock: trả về existingTemplate
            _biometricRepository.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<BiometricTemplate, bool>>>(),
                Arg.Any<System.Threading.CancellationToken>()
            ).ReturnsForAnyArgs(Task.FromResult(existingTemplate));

            // Setup mock Update
            BiometricTemplate updatedEntity = null!;
            _biometricRepository.UpdateAsync(Arg.Do<BiometricTemplate>(t => updatedEntity = t)).Returns(callInfo => Task.FromResult(callInfo.Arg<BiometricTemplate>()));

            // Act
            int updateCount = await _biometricAppService.UploadTemplatesAsync(new List<BiometricTemplateDto> { updatedDto });

            // Assert
            updateCount.ShouldBe(1);
            updatedEntity.ShouldNotBeNull();
            updatedEntity.TemplateData.ShouldBe(REAL_ZKT_FINGER_BASE64);
        }
    }
}
