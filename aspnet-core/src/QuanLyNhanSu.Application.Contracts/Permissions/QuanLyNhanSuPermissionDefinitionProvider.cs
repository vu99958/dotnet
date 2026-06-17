using QuanLyNhanSu.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace QuanLyNhanSu.Permissions;

public class QuanLyNhanSuPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(QuanLyNhanSuPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(QuanLyNhanSuPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<QuanLyNhanSuResource>(name);
    }
}
