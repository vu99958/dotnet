using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using QuanLyNhanSu.Settings;
using QuanLyNhanSu;

namespace QuanLyNhanSu.Payroll.BackgroundJobs
{
    public class PayrollCalculationJob : AsyncBackgroundJob<PayrollCalculationJobArgs>, ITransientDependency
    {
        private readonly IRepository<SalaryProfile, Guid> _salaryProfileRepository;
        private readonly IRepository<AttendanceRecord, Guid> _attendanceRepository;
        private readonly IRepository<LeaveRequest, Guid> _leaveRequestRepository;
        private readonly IRepository<Payslip, Guid> _payslipRepository;
        private readonly ISettingProvider _settingProvider;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<PayrollCalculationJob> _logger;

        public PayrollCalculationJob(
            IRepository<SalaryProfile, Guid> salaryProfileRepository,
            IRepository<AttendanceRecord, Guid> attendanceRepository,
            IRepository<LeaveRequest, Guid> leaveRequestRepository,
            IRepository<Payslip, Guid> payslipRepository,
            ISettingProvider settingProvider,
            IGuidGenerator guidGenerator,
            ILogger<PayrollCalculationJob> logger)
        {
            _salaryProfileRepository = salaryProfileRepository;
            _attendanceRepository = attendanceRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _payslipRepository = payslipRepository;
            _settingProvider = settingProvider;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        [UnitOfWork]
        public override async Task ExecuteAsync(PayrollCalculationJobArgs args)
        {
            _logger.LogInformation($"[PayrollJob] Bắt đầu tính lương tháng {args.Month}/{args.Year}...");

            var startDate = new DateTime(args.Year, args.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            int standardWorkDays = CalculateStandardWorkDays(args.Month, args.Year);

            var latePenaltySetting = await _settingProvider.GetOrNullAsync(QuanLyNhanSuSettings.Payroll.LatePenaltyPerMinute) ?? "2000";
            var netSalaryRateSetting = await _settingProvider.GetOrNullAsync(QuanLyNhanSuSettings.Payroll.NetSalaryRate) ?? "0.895";
            
            decimal latePenaltyPerMinute = decimal.Parse(latePenaltySetting);
            decimal netSalaryRate = decimal.Parse(netSalaryRateSetting);

            var profileQueryable = await _salaryProfileRepository.GetQueryableAsync();
            int totalProfiles = profileQueryable.Count();
            int batchSize = 100;

            for (int skip = 0; skip < totalProfiles; skip += batchSize)
            {
                var batchProfiles = profileQueryable.Skip(skip).Take(batchSize).ToList();
                var batchUserIds = batchProfiles.Select(p => p.UserId).ToList();

                var attendances = await _attendanceRepository.GetListAsync(x => 
                    x.WorkDate >= startDate && x.WorkDate <= endDate && batchUserIds.Contains(x.UserId));
                
                var leaves = await _leaveRequestRepository.GetListAsync(x =>
                    x.Status == QuanLyNhanSu.Enums.LeaveRequestStatus.Approved &&
                    x.StartDate <= endDate && x.EndDate >= startDate && batchUserIds.Contains(x.UserId));
                
                var existingPayslips = await _payslipRepository.GetListAsync(x => 
                    x.Month == args.Month && x.Year == args.Year && batchUserIds.Contains(x.UserId));

                var attendanceLookup = attendances.Where(x => x.CheckInTime != null).ToLookup(x => x.UserId);
                var leaveLookup = leaves.ToLookup(x => x.UserId);
                var existingPayslipsDict = existingPayslips.ToDictionary(x => x.UserId);

                var payslipsToInsert = new List<Payslip>();
                var payslipsToUpdate = new List<Payslip>();

                foreach (var profile in batchProfiles)
                {
                    var userId = profile.UserId;
                    var userAttendances = attendanceLookup[userId].ToList();
                    
                    double actualWorkDays = 0;
                    foreach (var att in userAttendances)
                    {
                        if (att.CheckInTime != null && att.CheckOutTime != null)
                        {
                            actualWorkDays += 1.0;
                        }
                        else if (att.CheckInTime != null && att.CheckOutTime == null)
                        {
                            actualWorkDays += 0.5; 
                        }
                    }

                    int totalLateMinutes = userAttendances.Sum(x => x.LateMinutes);
                    int totalEarlyMinutes = userAttendances.Sum(x => x.EarlyLeaveMinutes);
                    decimal totalPenalty = (totalLateMinutes + totalEarlyMinutes) * latePenaltyPerMinute;

                    var userLeaves = leaveLookup[userId].ToList();
                    int approvedLeaveDays = 0;
                    foreach (var leave in userLeaves)
                    {
                        var leaveStart = leave.StartDate.Date > startDate ? leave.StartDate.Date : startDate;
                        var leaveEnd = leave.EndDate.Date < endDate ? leave.EndDate.Date : endDate;
                        if (leaveEnd >= leaveStart)
                        {
                            for (var d = leaveStart; d <= leaveEnd; d = d.AddDays(1))
                            {
                                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                                {
                                    approvedLeaveDays++;
                                }
                            }
                        }
                    }

                    decimal dailySalary = profile.BaseSalary / standardWorkDays;
                    double totalPaidDays = actualWorkDays + approvedLeaveDays;
                    
                    if (totalPaidDays > standardWorkDays)
                    {
                        totalPaidDays = standardWorkDays;
                    }

                    int overtimeDays = 0;
                    decimal overtimePay = 0;

                    decimal regularDays = (decimal)totalPaidDays;
                    decimal grossSalary = (dailySalary * regularDays) + profile.Allowance + overtimePay - totalPenalty;
                    if (grossSalary < 0) grossSalary = 0;
                    decimal netSalary = grossSalary * netSalaryRate;

                    if (!existingPayslipsDict.TryGetValue(userId, out var payslip))
                    {
                        payslip = new Payslip(
                            _guidGenerator.Create(),
                            userId, args.Month, args.Year,
                            standardWorkDays, actualWorkDays, approvedLeaveDays,
                            overtimeDays, overtimePay,
                            totalPenalty, grossSalary, netSalary
                        );
                        payslipsToInsert.Add(payslip);
                    }
                    else
                    {
                        payslip.StandardWorkDays = standardWorkDays;
                        payslip.ActualWorkDays = actualWorkDays;
                        payslip.ApprovedLeaveDays = approvedLeaveDays;
                        payslip.OvertimeDays = overtimeDays;
                        payslip.OvertimePay = overtimePay;
                        payslip.TotalPenalty = totalPenalty;
                        payslip.GrossSalary = grossSalary;
                        payslip.NetSalary = netSalary;
                        payslipsToUpdate.Add(payslip);
                    }
                }

                if (payslipsToInsert.Any())
                {
                    await _payslipRepository.InsertManyAsync(payslipsToInsert);
                }
                if (payslipsToUpdate.Any())
                {
                    await _payslipRepository.UpdateManyAsync(payslipsToUpdate);
                }
            }

            _logger.LogInformation($"[PayrollJob] Hoàn tất tính lương tháng {args.Month}/{args.Year}.");
        }

        private int CalculateStandardWorkDays(int month, int year)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int standardDays = 0;
            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(year, month, i);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    standardDays++;
                }
            }
            return standardDays;
        }
    }
}
