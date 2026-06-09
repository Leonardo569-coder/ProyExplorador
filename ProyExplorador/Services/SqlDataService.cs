using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace ProyExplorador.Services
{
    /// <summary>
    /// Servicio para consultar datos desde SQL Server.
    /// </summary>
    public class SqlDataService
    {
        private readonly string _connectionString;

        public SqlDataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Obtiene un DataTable con los resultados de una consulta SQL.
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            var dataTable = new DataTable();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 60;
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al ejecutar consulta SQL: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Obtiene todas las filas de una tabla específica.
        /// </summary>
        public async Task<DataTable> GetTableAsync(string tableName)
        {
            var query = $"SELECT * FROM [{tableName}]";
            return await ExecuteQueryAsync(query);
        }

        /// <summary>
        /// Obtiene las primeras N filas de una tabla.
        /// </summary>
        public async Task<DataTable> GetTableAsync(string tableName, int topRows)
        {
            var query = $"SELECT TOP {topRows} * FROM [{tableName}]";
            return await ExecuteQueryAsync(query);
        }

        /// <summary>
        /// Prueba la conexión a SQL Server.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene la lista de tablas disponibles.
        /// </summary>
        public async Task<DataTable> GetTablesListAsync()
        {
            var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            return await ExecuteQueryAsync(query);
        }
    }
}
