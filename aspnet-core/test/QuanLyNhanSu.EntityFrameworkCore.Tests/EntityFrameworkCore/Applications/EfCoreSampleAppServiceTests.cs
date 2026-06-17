using QuanLyNhanSu.Samples;
using Xunit;

namespace QuanLyNhanSu.EntityFrameworkCore.Applications;

[Collection(QuanLyNhanSuTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<QuanLyNhanSuEntityFrameworkCoreTestModule>
{

}
