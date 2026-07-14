using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace QuanLyNhanSu.Domain.Shifts
{
    public class Shift : FullAuditedEntity<Guid>
    {
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public bool IsNightShift { get; private set; }
        public int AllowedLateMinutes { get; private set; }
        public decimal DeductAmountPerLateMinute { get; private set; }

        protected Shift() { }

        public Shift(Guid id, string code, string name, TimeSpan startTime, TimeSpan endTime, bool isNightShift, int allowedLateMinutes, decimal deductAmount)
            : base(id)
        {
            Code = code;
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
            IsNightShift = isNightShift;
            AllowedLateMinutes = allowedLateMinutes;
            DeductAmountPerLateMinute = deductAmount;
        }

        public void Update(string name, TimeSpan startTime, TimeSpan endTime, bool isNightShift, int allowedLateMinutes, decimal deductAmount)
        {
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
            IsNightShift = isNightShift;
            AllowedLateMinutes = allowedLateMinutes;
            DeductAmountPerLateMinute = deductAmount;
        }
    }
}
