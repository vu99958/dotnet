using Xunit;

namespace QuanLyNhanSu.EntityFrameworkCore;

[CollectionDefinition(QuanLyNhanSuTestConsts.CollectionDefinitionName)]
public class QuanLyNhanSuEntityFrameworkCoreCollection : ICollectionFixture<QuanLyNhanSuEntityFrameworkCoreFixture>
{

}
