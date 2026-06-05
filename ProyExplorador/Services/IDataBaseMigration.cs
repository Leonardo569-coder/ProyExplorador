using System.Threading.Tasks;

namespace ProyExplorador.Services
{
    public interface IDataBaseMigration
    {
        Task<(bool Success, string Message)> MigrateFromCsvAsync(string filePath, string tableName);
        Task<bool> TestConnectionAsync();
    }
}
