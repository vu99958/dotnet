using QuanLyNhanSu.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace QuanLyNhanSu.Permissions;

public class QuanLyNhanSuPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(QuanLyNhanSuPermissions.GroupName);

        // Attendance
        var attendancePermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Attendance.Default, L("Permission:Attendance"));
        attendancePermission.AddChild(QuanLyNhanSuPermissions.Attendance.Manage, L("Permission:Attendance.Manage"));

        // Dashboard
        var dashboardPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Dashboard.Default, L("Permission:Dashboard"));
        dashboardPermission.AddChild(QuanLyNhanSuPermissions.Dashboard.Manage, L("Permission:Dashboard.Manage"));

        // Biometric
        var biometricPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Biometric.Default, L("Permission:Biometric"));
        biometricPermission.AddChild(QuanLyNhanSuPermissions.Biometric.Manage, L("Permission:Biometric.Manage"));

        // Payslip
        var payslipPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Payslip.Default, L("Permission:Payslip"));
        payslipPermission.AddChild(QuanLyNhanSuPermissions.Payslip.Manage, L("Permission:Payslip.Manage"));

        // UserKey
        var userKeyPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.UserKey.Default, L("Permission:UserKey"));
        userKeyPermission.AddChild(QuanLyNhanSuPermissions.UserKey.Manage, L("Permission:UserKey.Manage"));

        // Branch
        var branchPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Branch.Default, L("Permission:Branch"));
        branchPermission.AddChild(QuanLyNhanSuPermissions.Branch.Manage, L("Permission:Branch.Manage"));

        // Employee
        var employeePermission = myGroup.AddPermission(QuanLyNhanSuPermissions.Employee.Default, L("Permission:Employee"));
        employeePermission.AddChild(QuanLyNhanSuPermissions.Employee.Manage, L("Permission:Employee.Manage"));

        // SalaryProfile
        var salaryProfilePermission = myGroup.AddPermission(QuanLyNhanSuPermissions.SalaryProfile.Default, L("Permission:SalaryProfile"));
        salaryProfilePermission.AddChild(QuanLyNhanSuPermissions.SalaryProfile.Manage, L("Permission:SalaryProfile.Manage"));

        // LeaveRequest
        var leaveRequestPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.LeaveRequest.Default, L("Permission:LeaveRequest"));
        leaveRequestPermission.AddChild(QuanLyNhanSuPermissions.LeaveRequest.Manage, L("Permission:LeaveRequest.Manage"));

        // PayslipComplaint
        var payslipComplaintPermission = myGroup.AddPermission(QuanLyNhanSuPermissions.PayslipComplaint.Default, L("Permission:PayslipComplaint"));
        payslipComplaintPermission.AddChild(QuanLyNhanSuPermissions.PayslipComplaint.Manage, L("Permission:PayslipComplaint.Manage"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<QuanLyNhanSuResource>(name);
    }
}
