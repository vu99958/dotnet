using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NSubstitute;
using QuanLyNhanSu.Domain;
using QuanLyNhanSu.Services;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace QuanLyNhanSu.Tests
{
    /// <summary>
    /// Unit Test kiểm chứng tính năng Đồng bộ dữ liệu Chấm công (Real Data Format).
    /// </summary>
    public class AttendanceAppServiceTests
    {
        private readonly AttendanceAppService _attendanceAppService;
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<UserKey, Guid> _userKeyRepository;
        private readonly IRepository<Branch, Guid> _branchRepository;
        private readonly IRepository<LeaveRequest, Guid> _leaveRequestRepository;
        private readonly AttendanceManager _attendanceManager;
        private readonly IGuidGenerator _guidGenerator;
        private Volo.Abp.EventBus.Distributed.IDistributedEventBus _distributedEventBus;

        public AttendanceAppServiceTests()
        {
            _attendanceRepository = Substitute.For<IRepository<AttendanceRecord, Guid>>();
            _userRepository = Substitute.For<IRepository<IdentityUser, Guid>>();
            _userKeyRepository = Substitute.For<IRepository<UserKey, Guid>>();
            _branchRepository = Substitute.For<IRepository<Branch, Guid>>();
            _leaveRequestRepository = Substitute.For<IRepository<LeaveRequest, Guid>>();
            _attendanceManager = Substitute.ForPartsOf<AttendanceManager>(_branchRepository);
            
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(Guid.NewGuid());
            
            var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
            lazyServiceProvider.LazyGetService<IGuidGenerator>().Returns(_guidGenerator);

            _distributedEventBus = Substitute.For<Volo.Abp.EventBus.Distributed.IDistributedEventBus>();

            _attendanceAppService = new AttendanceAppService(
                _attendanceRepository,
                _userRepository,
                _userKeyRepository,
                _branchRepository,
                _leaveRequestRepository,
                _attendanceManager,
                _distributedEventBus
            )
            {
                LazyServiceProvider = lazyServiceProvider
            };
        }

        [Fact]
        public async Task SyncBulkDataAsync_Should_Publish_Event()
        {
            // Arrange
            var inputList = new List<SyncAttendanceDto>
            {
                new SyncAttendanceDto
                {
                    UserName = "100",
                    TimeStamp = new DateTime(2026, 7, 10, 7, 50, 0), // IN
                    CheckType = "IN"
                },
                new SyncAttendanceDto
                {
                    UserName = "100",
                    TimeStamp = new DateTime(2026, 7, 10, 17, 30, 0), // OUT
                    CheckType = "OUT"
                }
            };

            // Act
            int count = await _attendanceAppService.SyncBulkDataAsync(inputList);

            // Assert
            count.ShouldBe(2); // Trả về số lượng item nhận được
            await _distributedEventBus.Received(1).PublishAsync(
                Arg.Is<QuanLyNhanSu.Attendance.AttendancePushedEto>(e => 
                    e.Logs.Count == 2 && 
                    e.Logs[0].UserName == "100"
                )
            );
        }
    }
}
