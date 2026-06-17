using Volo.Abp.Modularity;

namespace QuanLyNhanSu;

[DependsOn(
    typeof(QuanLyNhanSuApplicationModule),
    typeof(QuanLyNhanSuDomainTestModule)
)]
public class QuanLyNhanSuApplicationTestModule : AbpModule
{

}
