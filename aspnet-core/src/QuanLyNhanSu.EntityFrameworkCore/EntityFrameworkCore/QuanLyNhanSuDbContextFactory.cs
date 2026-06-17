using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QuanLyNhanSu.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class QuanLyNhanSuDbContextFactory : IDesignTimeDbContextFactory<QuanLyNhanSuDbContext>
{
    public QuanLyNhanSuDbContext CreateDbContext(string[] args)
    {
        QuanLyNhanSuEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<QuanLyNhanSuDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new QuanLyNhanSuDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../QuanLyNhanSu.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
