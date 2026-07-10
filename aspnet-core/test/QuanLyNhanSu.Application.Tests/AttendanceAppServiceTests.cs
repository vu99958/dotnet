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
using Volo.Abp.Identity;
using Xunit;

namespace QuanLyNhanSu
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
        private readonly IGuidGenerator _guidGenerator;

        public AttendanceAppServiceTests()
        {
            _attendanceRepository = Substitute.For<IRepository<AttendanceRecord, Guid>>();
            _userRepository = Substitute.For<IRepository<IdentityUser, Guid>>();
            _userKeyRepository = Substitute.For<IRepository<UserKey, Guid>>();
            _branchRepository = Substitute.For<IRepository<Branch, Guid>>();
            _leaveRequestRepository = Substitute.For<IRepository<LeaveRequest, Guid>>();
            
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(Guid.NewGuid());

            var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
            lazyServiceProvider.LazyGetService<IGuidGenerator>().Returns(_guidGenerator);

            _attendanceAppService = new AttendanceAppService(
                _attendanceRepository,
                _userRepository,
                _userKeyRepository,
                _branchRepository,
                _leaveRequestRepository
            )
            {
                LazyServiceProvider = lazyServiceProvider
            };
        }

        [Fact]
        public async Task SyncBulkDataAsync_Should_Save_New_Logs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            // Cấu hình mock: Trả về 1 user giả
            var mockUser = new IdentityUser(userId, "100", "nv100@abp.io");
            _userRepository.GetListAsync(Arg.Any<Expression<Func<IdentityUser, bool>>>()).Returns(Task.FromResult(new List<IdentityUser> { mockUser }));

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

            var insertedRecords = new List<IEnumerable<AttendanceRecord>>();
            _attendanceRepository.InsertManyAsync(Arg.Do<IEnumerable<AttendanceRecord>>(r => insertedRecords.Add(r))).Returns(Task.CompletedTask);

            // Act
            int savedCount = await _attendanceAppService.SyncBulkDataAsync(inputList);

            // Assert
            savedCount.ShouldBe(1); // 1 record tổng hợp trong ngày cho user 100
            insertedRecords.ShouldNotBeEmpty();
            
            var record = insertedRecords.First().First();
            record.CheckInTime.ShouldNotBeNull();
            record.CheckInTime.Value.Hour.ShouldBe(7);
            record.CheckOutTime.ShouldNotBeNull();
            record.CheckOutTime.Value.Hour.ShouldBe(17);
        }

        [Fact]
        public async Task SyncBulkDataAsync_Should_Calculate_Late_And_Early()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUser = new IdentityUser(userId, "200", "nv200@abp.io");
            _userRepository.GetListAsync(Arg.Any<Expression<Func<IdentityUser, bool>>>()).Returns(Task.FromResult(new List<IdentityUser> { mockUser }));

            var inputList = new List<SyncAttendanceDto>
            {
                new SyncAttendanceDto
                {
                    UserName = "200",
                    TimeStamp = new DateTime(2026, 7, 11, 8, 30, 0), // Đi trễ 30 phút
                    CheckType = "IN"
                },
                new SyncAttendanceDto
                {
                    UserName = "200",
                    TimeStamp = new DateTime(2026, 7, 11, 16, 45, 0), // Về sớm 15 phút
                    CheckType = "OUT"
                }
            };

            var insertedRecords = new List<IEnumerable<AttendanceRecord>>();
            _attendanceRepository.InsertManyAsync(Arg.Do<IEnumerable<AttendanceRecord>>(r => insertedRecords.Add(r))).Returns(Task.CompletedTask);

            // Act
            await _attendanceAppService.SyncBulkDataAsync(inputList);

            // Assert
            var record = insertedRecords.First().First();
            record.LateMinutes.ShouldBe(30);
            record.EarlyLeaveMinutes.ShouldBe(15);
        }
    }
}
