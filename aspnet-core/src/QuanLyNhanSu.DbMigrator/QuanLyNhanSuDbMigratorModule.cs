using QuanLyNhanSu.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QuanLyNhanSu.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(QuanLyNhanSuEntityFrameworkCoreModule),
    typeof(QuanLyNhanSuApplicationContractsModule)
    )]
public class QuanLyNhanSuDbMigratorModule : AbpModule
{
}
