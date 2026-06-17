using Volo.Abp.Modularity;

namespace QuanLyNhanSu;

/* Inherit from this class for your domain layer tests. */
public abstract class QuanLyNhanSuDomainTestBase<TStartupModule> : QuanLyNhanSuTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
