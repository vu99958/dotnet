using System.Threading.Tasks;

namespace QuanLyNhanSu.Data;

public interface IQuanLyNhanSuDbSchemaMigrator
{
    Task MigrateAsync();
}
