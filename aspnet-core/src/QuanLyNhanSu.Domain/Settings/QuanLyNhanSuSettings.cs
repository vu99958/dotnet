namespace QuanLyNhanSu.Settings;

public static class QuanLyNhanSuSettings
{
    private const string Prefix = "QuanLyNhanSu";

    public static class Payroll
    {
        public const string LatePenaltyPerMinute = Prefix + ".Payroll.LatePenaltyPerMinute";
        public const string NetSalaryRate = Prefix + ".Payroll.NetSalaryRate";
    }
}
