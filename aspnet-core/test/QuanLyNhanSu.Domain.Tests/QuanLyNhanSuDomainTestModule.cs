using Volo.Abp.Modularity;

namespace QuanLyNhanSu;

[DependsOn(
    typeof(QuanLyNhanSuDomainModule),
    typeof(QuanLyNhanSuTestBaseModule)
)]
public class QuanLyNhanSuDomainTestModule : AbpModule
{

}
