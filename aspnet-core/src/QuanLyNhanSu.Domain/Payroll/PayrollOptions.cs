using System.Collections.Generic;

namespace QuanLyNhanSu.Domain.Payroll
{
    public class PayrollOptions
    {
        // Tỷ lệ khấu trừ người lao động
        public decimal SocialInsuranceRate { get; set; } = 0.08m; // BHXH 8%
        public decimal HealthInsuranceRate { get; set; } = 0.015m; // BHYT 1.5%
        public decimal UnemploymentInsuranceRate { get; set; } = 0.01m; // BHTN 1%

        // Giảm trừ gia cảnh
        public decimal PersonalDeduction { get; set; } = 11_000_000m;
        public decimal DependentDeduction { get; set; } = 4_400_000m;

        // Biểu thuế lũy tiến từng phần
        public List<TaxBracket> ProgressiveTaxBrackets { get; set; } = new List<TaxBracket>();
        
        public PayrollOptions()
        {
            // Khởi tạo mặc định 7 bậc thuế của Việt Nam
            ProgressiveTaxBrackets.Add(new TaxBracket(5_000_000m, 0.05m));
            ProgressiveTaxBrackets.Add(new TaxBracket(10_000_000m, 0.10m));
            ProgressiveTaxBrackets.Add(new TaxBracket(18_000_000m, 0.15m));
            ProgressiveTaxBrackets.Add(new TaxBracket(32_000_000m, 0.20m));
            ProgressiveTaxBrackets.Add(new TaxBracket(52_000_000m, 0.25m));
            ProgressiveTaxBrackets.Add(new TaxBracket(80_000_000m, 0.30m));
            ProgressiveTaxBrackets.Add(new TaxBracket(null, 0.35m));
        }
    }
}
