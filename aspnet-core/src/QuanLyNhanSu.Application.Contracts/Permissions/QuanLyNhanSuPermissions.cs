namespace QuanLyNhanSu.Permissions;

public static class QuanLyNhanSuPermissions
{
    public const string GroupName = "QuanLyNhanSu";

    public static class Attendance
    {
        public const string Default = GroupName + ".Attendance";
        public const string Manage = Default + ".Manage";
    }

    public static class Dashboard
    {
        public const string Default = GroupName + ".Dashboard";
        public const string Manage = Default + ".Manage";
    }

    public static class Biometric
    {
        public const string Default = GroupName + ".Biometric";
        public const string Manage = Default + ".Manage";
    }

    public static class Payslip
    {
        public const string Default = GroupName + ".Payslip";
        public const string Manage = Default + ".Manage";
    }

    public static class UserKey
    {
        public const string Default = GroupName + ".UserKey";
        public const string Manage = Default + ".Manage";
    }

    public static class Branch
    {
        public const string Default = GroupName + ".Branch";
        public const string Manage = Default + ".Manage";
    }

    public static class Employee
    {
        public const string Default = GroupName + ".Employee";
        public const string Manage = Default + ".Manage";
    }

    public static class SalaryProfile
    {
        public const string Default = GroupName + ".SalaryProfile";
        public const string Manage = Default + ".Manage";
    }

    public static class LeaveRequest
    {
        public const string Default = GroupName + ".LeaveRequest";
        public const string Manage = Default + ".Manage";
    }

    public static class PayslipComplaint
    {
        public const string Default = GroupName + ".PayslipComplaint";
        public const string Manage = Default + ".Manage";
    }
}
