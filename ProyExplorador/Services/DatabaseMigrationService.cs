using Microsoft.Extensions.Configuration;

namespace ProyExplorador.Services
{
    public class DatabaseMigrationService
    {
        private readonly string _mySqlConnection;
        private readonly string _sqlServerConnection;

        public DatabaseMigrationService()
        {
            var config = App.Configuration;
            _mySqlConnection = config.GetConnectionString("MySqlConnection");
            _sqlServerConnection = config.GetConnectionString("SqlServerConnection");
        }

        public string GetMySqlConnection() => _mySqlConnection;
        public string GetSqlServerConnection() => _sqlServerConnection;
    }
}
