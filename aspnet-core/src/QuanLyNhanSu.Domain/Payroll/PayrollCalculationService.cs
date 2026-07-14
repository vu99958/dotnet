using System;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Services;

namespace QuanLyNhanSu.Domain.Payroll
{
    public class PayrollCalculationService : DomainService
    {
        private readonly PayrollOptions _options;

        public PayrollCalculationService(IOptions<PayrollOptions> options)
        {
            _options = options.Value;
        }

        public PayrollResult CalculateNetSalary(decimal grossSalary, int dependentCount, decimal insuranceSalaryBase)
        {
            // 1. Tính bảo hiểm bắt buộc
            decimal totalInsurance = (insuranceSalaryBase * _options.SocialInsuranceRate)
                                   + (insuranceSalaryBase * _options.HealthInsuranceRate)
                                   + (insuranceSalaryBase * _options.UnemploymentInsuranceRate);

            // 2. Tính Thu nhập trước thuế
            decimal incomeBeforeTax = grossSalary - totalInsurance;

            // 3. Khấu trừ gia cảnh để ra Thu nhập tính thuế (Taxable Income)
            decimal totalDeduction = _options.PersonalDeduction + (dependentCount * _options.DependentDeduction);
            decimal taxableIncome = Math.Max(0, incomeBeforeTax - totalDeduction);

            // 4. Tính thuế lũy tiến từng phần (Progressive Tax)
            decimal totalTax = CalculateProgressiveTax(taxableIncome);

            // 5. Kết quả
            return new PayrollResult
            {
                GrossSalary = grossSalary,
                TotalInsuranceAmount = totalInsurance,
                PitAmount = totalTax,
                NetSalary = grossSalary - totalInsurance - totalTax
            };
        }

        // [ONBOARDING COMMENT]: Thuật toán tính thuế lũy tiến
        // Ta duyệt qua từng bậc thuế. Nếu thu nhập tính thuế vượt qua bậc hiện tại, 
        // lấy phần chênh lệch (Income In Bracket) nhân với % Thuế suất của bậc đó, 
        // sau đó trừ đi phần đã tính từ tổng thu nhập chưa tính thuế để sang bậc tiếp theo.
        private decimal CalculateProgressiveTax(decimal taxableIncome)
        {
            if (taxableIncome <= 0 || _options.ProgressiveTaxBrackets == null || _options.ProgressiveTaxBrackets.Count == 0)
                return 0;

            decimal tax = 0;
            decimal remainingIncome = taxableIncome;
            decimal previousBracketLimit = 0;

            foreach (var bracket in _options.ProgressiveTaxBrackets)
            {
                if (remainingIncome <= 0) break;

                // Độ rộng của bậc (Ví dụ bậc 1: 5tr, bậc 2: 5tr (10-5), v.v.)
                decimal bracketWidth = bracket.UpToIncome.HasValue 
                    ? bracket.UpToIncome.Value - previousBracketLimit 
                    : decimal.MaxValue;

                decimal incomeInThisBracket = Math.Min(remainingIncome, bracketWidth);
                
                tax += incomeInThisBracket * bracket.TaxRate;
                
                remainingIncome -= incomeInThisBracket;
                previousBracketLimit = bracket.UpToIncome ?? 0;
            }

            return tax;
        }
    }
}
