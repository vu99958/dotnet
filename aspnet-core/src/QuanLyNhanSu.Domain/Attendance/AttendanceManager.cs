using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain.Shifts;

namespace QuanLyNhanSu.Domain.Attendance
{
    public class AttendanceManager : DomainService
    {
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<EmployeeShift, Guid> _employeeShiftRepository;
        private readonly IRepository<Shift, Guid> _shiftRepository;

        public AttendanceManager(
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<EmployeeShift, Guid> employeeShiftRepository,
            IRepository<Shift, Guid> shiftRepository)
        {
            _attendanceRepository = attendanceRepository;
            _employeeShiftRepository = employeeShiftRepository;
            _shiftRepository = shiftRepository;
        }

        public async Task<AttendanceRecord> ProcessCheckInAsync(Guid userId, DateTime checkTime)
        {
            var workDate = checkTime.Date;
            
            // Lấy ca làm việc đã xếp cho ngày hôm nay
            var employeeShift = await _employeeShiftRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.WorkDate == workDate);
            if (employeeShift == null)
            {
                throw new ApplicationException("Không tìm thấy ca làm việc được phân công cho ngày này.");
            }

            var shift = await _shiftRepository.GetAsync(employeeShift.ShiftId);

            var record = await _attendanceRepository.FirstOrDefaultAsync(x => x.UserId == userId && x.WorkDate == workDate);
            if (record == null)
            {
                record = new AttendanceRecord(Guid.NewGuid(), userId, workDate, "Đúng giờ");
                record.CheckInTime = checkTime;

                // Tính toán đi trễ
                var expectedCheckIn = workDate.Add(shift.StartTime);
                var diff = checkTime - expectedCheckIn;
                
                if (diff.TotalMinutes > shift.AllowedLateMinutes)
                {
                    record.LateMinutes = (int)diff.TotalMinutes;
                    record.Status = "Đi trễ";
                }

                await _attendanceRepository.InsertAsync(record, autoSave: true);
            }
            else
            {
                // Xử lý luồng Check-out nếu đã có Check-in
                record.CheckOutTime = checkTime;
                
                // Tính toán về sớm (nếu checkTime < EndTime)
                var expectedCheckOut = workDate.Add(shift.EndTime);
                if (shift.IsNightShift && shift.EndTime < shift.StartTime)
                {
                    expectedCheckOut = expectedCheckOut.AddDays(1);
                }

                var earlyDiff = expectedCheckOut - checkTime;
                if (earlyDiff.TotalMinutes > 0)
                {
                    record.EarlyLeaveMinutes = (int)earlyDiff.TotalMinutes;
                    if (record.Status == "Đúng giờ")
                    {
                        record.Status = "Về sớm";
                    }
                    else if (record.Status == "Đi trễ")
                    {
                        record.Status = "Đi trễ & Về sớm";
                    }
                }

                await _attendanceRepository.UpdateAsync(record, autoSave: true);
            }

            return record;
        }
    }
}
