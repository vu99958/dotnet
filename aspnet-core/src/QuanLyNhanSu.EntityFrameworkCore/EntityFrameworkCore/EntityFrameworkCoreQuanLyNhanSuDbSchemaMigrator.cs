using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyNhanSu.Data;
using Volo.Abp.DependencyInjection;

namespace QuanLyNhanSu.EntityFrameworkCore;

public class EntityFrameworkCoreQuanLyNhanSuDbSchemaMigrator
    : IQuanLyNhanSuDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreQuanLyNhanSuDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the QuanLyNhanSuDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<QuanLyNhanSuDbContext>()
            .Database
            .MigrateAsync();
    }
}
