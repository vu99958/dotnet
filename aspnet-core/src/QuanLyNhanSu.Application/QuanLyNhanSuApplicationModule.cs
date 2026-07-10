using Volo.Abp.Account;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyNhanSu;

[DependsOn(
    typeof(QuanLyNhanSuDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(QuanLyNhanSuApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class QuanLyNhanSuApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMapperlyObjectMapper<QuanLyNhanSuApplicationModule>();
    }
}
