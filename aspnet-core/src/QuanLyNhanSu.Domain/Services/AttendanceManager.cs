using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Domain.Repositories;
using QuanLyNhanSu.Domain;
using Volo.Abp;

namespace QuanLyNhanSu.Services
{
    public class AttendanceManager : DomainService
    {
        private readonly IRepository<Branch, Guid> _branchRepository;

        public AttendanceManager(IRepository<Branch, Guid> branchRepository)
        {
            _branchRepository = branchRepository;
        }

        public virtual async Task<double> CalculateDistanceAsync(Guid branchId, double userLat, double userLng)
        {
            var matchedBranch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId);
            if (matchedBranch == null)
            {
                throw new UserFriendlyException("Chi nhánh phân bổ không tồn tại hoặc đã bị xóa!");
            }

            return CalculateDistanceInMeters(userLat, userLng, matchedBranch.Latitude, matchedBranch.Longitude);
        }

        public virtual (int LateMinutes, int EarlyMinutes) CalculateLateAndEarly(DateTime? checkIn, DateTime? checkOut, DateTime shiftStart, DateTime shiftEnd)
        {
            int lateMinutes = 0;
            int earlyMinutes = 0;

            if (checkIn.HasValue && checkIn.Value > shiftStart)
            {
                lateMinutes = (int)(checkIn.Value - shiftStart).TotalMinutes;
            }

            if (checkOut.HasValue && checkOut.Value < shiftEnd)
            {
                earlyMinutes = (int)(shiftEnd - checkOut.Value).TotalMinutes;
            }

            return (lateMinutes, earlyMinutes);
        }

        private double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // metres
            var φ1 = lat1 * Math.PI / 180; // φ, λ in radians
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // in metres
        }
    }
}
