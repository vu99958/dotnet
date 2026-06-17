using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QuanLyNhanSu.Data;

/* This is used if database provider does't define
 * IQuanLyNhanSuDbSchemaMigrator implementation.
 */
public class NullQuanLyNhanSuDbSchemaMigrator : IQuanLyNhanSuDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
