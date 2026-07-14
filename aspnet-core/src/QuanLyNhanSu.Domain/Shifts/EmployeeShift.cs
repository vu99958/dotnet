using System;
using Volo.Abp.Domain.Entities;

namespace QuanLyNhanSu.Domain.Shifts
{
    public class EmployeeShift : Entity<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid ShiftId { get; private set; }
        public DateTime WorkDate { get; private set; }

        protected EmployeeShift() { }

        public EmployeeShift(Guid id, Guid userId, Guid shiftId, DateTime workDate) : base(id)
        {
            UserId = userId;
            ShiftId = shiftId;
            WorkDate = workDate.Date;
        }
        
        public void UpdateShift(Guid shiftId)
        {
            ShiftId = shiftId;
        }
    }
}
