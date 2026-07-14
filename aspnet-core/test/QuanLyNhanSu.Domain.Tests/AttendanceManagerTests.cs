using System;
using System.Threading.Tasks;
using NSubstitute;
using QuanLyNhanSu.Domain.Attendance;
using QuanLyNhanSu.Domain.Shifts;
using Volo.Abp.Domain.Repositories;
using Xunit;
using Shouldly;
using System.Linq.Expressions;
using System.Threading;

namespace QuanLyNhanSu.Domain.Tests
{
    public class AttendanceManagerTests
    {
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepoMock;
        private readonly IRepository<EmployeeShift, Guid> _employeeShiftRepoMock;
        private readonly IRepository<Shift, Guid> _shiftRepoMock;
        private readonly AttendanceManager _attendanceManager;

        public AttendanceManagerTests()
        {
            _attendanceRepoMock = Substitute.For<IRepository<AttendanceRecord, Guid>>();
            _employeeShiftRepoMock = Substitute.For<IRepository<EmployeeShift, Guid>>();
            _shiftRepoMock = Substitute.For<IRepository<Shift, Guid>>();

            _attendanceManager = new AttendanceManager(
                _attendanceRepoMock,
                _employeeShiftRepoMock,
                _shiftRepoMock
            );
        }

        [Fact]
        public async Task ProcessCheckInAsync_Should_ThrowException_When_NoShiftAssigned()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var checkTime = new DateTime(2026, 6, 20, 8, 0, 0);

            _employeeShiftRepoMock.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<EmployeeShift, bool>>>(),
                Arg.Any<CancellationToken>()
            ).Returns(Task.FromResult<EmployeeShift?>(null));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() =>
                _attendanceManager.ProcessCheckInAsync(userId, checkTime));
            
            ex.Message.ShouldBe("Không tìm thấy ca làm việc được phân công cho ngày này.");
        }

        [Fact]
        public async Task ProcessCheckInAsync_Should_CalculateLateMinutes_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workDate = new DateTime(2026, 6, 20);
            var checkTime = new DateTime(2026, 6, 20, 8, 10, 0); // Trễ 10 phút

            var shiftId = Guid.NewGuid();
            var shift = new Shift(shiftId, "HC", "Hành chính", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), false, 5, 10000);
            var employeeShift = new EmployeeShift(Guid.NewGuid(), userId, shiftId, workDate);

            _employeeShiftRepoMock.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<EmployeeShift, bool>>>(),
                Arg.Any<CancellationToken>()
            ).ReturnsForAnyArgs(Task.FromResult<EmployeeShift?>(employeeShift));

            _shiftRepoMock.GetAsync(shiftId).Returns(Task.FromResult(shift));

            _attendanceRepoMock.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<AttendanceRecord, bool>>>(),
                Arg.Any<CancellationToken>()
            ).ReturnsForAnyArgs(Task.FromResult<AttendanceRecord?>(null));

            // Act
            var record = await _attendanceManager.ProcessCheckInAsync(userId, checkTime);

            // Assert
            record.ShouldNotBeNull();
            record.LateMinutes.ShouldBe(10); // 08:10 - 08:00
            record.Status.ShouldBe("Đi trễ");
            await _attendanceRepoMock.Received(1).InsertAsync(record, autoSave: true);
        }
    }
}
