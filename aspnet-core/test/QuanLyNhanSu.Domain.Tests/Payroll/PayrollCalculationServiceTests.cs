using System;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuanLyNhanSu.Domain.Payroll;
using Xunit;

namespace QuanLyNhanSu.Domain.Tests.Payroll
{
    public class PayrollCalculationServiceTests
    {
        private readonly PayrollCalculationService _payrollService;
        private readonly PayrollOptions _options;

        public PayrollCalculationServiceTests()
        {
            _options = new PayrollOptions(); // Load default brackets (VN Tax)
            var mockOptions = Substitute.For<IOptions<PayrollOptions>>();
            mockOptions.Value.Returns(_options);
            
            _payrollService = new PayrollCalculationService(mockOptions);
        }

        [Fact]
        public void CalculateNetSalary_Should_NotChargeTax_When_IncomeIsLow()
        {
            // Arrange
            decimal grossSalary = 10_000_000m; // Lương 10tr
            int dependentCount = 0; // 0 người phụ thuộc
            decimal insuranceBase = grossSalary; // Đóng full bảo hiểm

            // Act
            var result = _payrollService.CalculateNetSalary(grossSalary, dependentCount, insuranceBase);

            // Assert
            // Bảo hiểm = 10tr * (8% + 1.5% + 1%) = 10tr * 10.5% = 1,050,000
            Assert.Equal(1_050_000m, result.TotalInsuranceAmount);

            // Thu nhập trước thuế = 10tr - 1.05tr = 8.95tr
            // Giảm trừ cá nhân = 11tr
            // Thu nhập tính thuế = 8.95tr - 11tr < 0 => Không đóng thuế
            Assert.Equal(0, result.PitAmount);
            Assert.Equal(8_950_000m, result.NetSalary);
        }

        [Fact]
        public void CalculateNetSalary_Should_CalculateCorrectly_With_Dependents()
        {
            // Arrange
            decimal grossSalary = 20_000_000m; // Lương 20tr
            int dependentCount = 1; // 1 người phụ thuộc
            decimal insuranceBase = grossSalary;

            // Act
            var result = _payrollService.CalculateNetSalary(grossSalary, dependentCount, insuranceBase);

            // Assert
            // Bảo hiểm = 20tr * 10.5% = 2,100,000
            Assert.Equal(2_100_000m, result.TotalInsuranceAmount);

            // Thu nhập trước thuế = 20tr - 2.1tr = 17.9tr
            // Giảm trừ: Bản thân (11tr) + 1 người phụ thuộc (4.4tr) = 15.4tr
            // Thu nhập tính thuế = 17.9tr - 15.4tr = 2.5tr
            //
            // Tính thuế:
            // Bậc 1 (<=5tr): 2.5tr * 5% = 125,000
            Assert.Equal(125_000m, result.PitAmount);
            
            // Lương Net = 20tr - 2.1tr - 125k = 17,775,000
            Assert.Equal(17_775_000m, result.NetSalary);
        }

        [Fact]
        public void CalculateNetSalary_Should_CalculateProgressiveTax_Correctly_ForHighIncome()
        {
            // Arrange
            decimal grossSalary = 40_000_000m; // Lương 40tr
            int dependentCount = 0; 
            decimal insuranceBase = grossSalary;

            // Act
            var result = _payrollService.CalculateNetSalary(grossSalary, dependentCount, insuranceBase);

            // Assert
            // Bảo hiểm = 40tr * 10.5% = 4,200,000
            // Thu nhập trước thuế = 40tr - 4.2tr = 35.8tr
            // Giảm trừ bản thân = 11tr
            // Thu nhập tính thuế (TNTT) = 35.8tr - 11tr = 24.8tr
            //
            // Thuế bậc 1 (0 -> 5tr): 5tr * 5% = 250,000
            // Thuế bậc 2 (5 -> 10tr): 5tr * 10% = 500,000
            // Thuế bậc 3 (10 -> 18tr): 8tr * 15% = 1,200,000
            // Thuế bậc 4 (18 -> 32tr): 6.8tr * 20% = 1,360,000
            // Tổng thuế = 250k + 500k + 1.2tr + 1.36tr = 3,310,000
            Assert.Equal(3_310_000m, result.PitAmount);
            
            // Net = 40tr - 4.2tr - 3.31tr = 32,490,000
            Assert.Equal(32_490_000m, result.NetSalary);
        }
    }
}
