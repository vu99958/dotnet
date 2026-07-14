using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace QuanLyNhanSu.Domain.Payroll
{
    public class TaxBracket : ValueObject
    {
        public decimal? UpToIncome { get; private set; } // Nếu null nghĩa là Không giới hạn (bậc cao nhất)
        public decimal TaxRate { get; private set; }

        public TaxBracket(decimal? upToIncome, decimal taxRate)
        {
            UpToIncome = upToIncome;
            TaxRate = taxRate;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return UpToIncome ?? 0;
            yield return TaxRate;
        }
    }
}
