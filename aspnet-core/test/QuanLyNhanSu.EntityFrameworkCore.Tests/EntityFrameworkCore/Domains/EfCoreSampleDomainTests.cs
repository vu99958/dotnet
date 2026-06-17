using QuanLyNhanSu.Samples;
using Xunit;

namespace QuanLyNhanSu.EntityFrameworkCore.Domains;

[Collection(QuanLyNhanSuTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<QuanLyNhanSuEntityFrameworkCoreTestModule>
{

}
