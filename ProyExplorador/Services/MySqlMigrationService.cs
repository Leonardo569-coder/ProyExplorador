using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using CsvHelper;
using System.Globalization;

namespace ProyExplorador.Services
{
    public class MySqlMigrationService : IDataBaseMigration
    {
        private readonly string _connectionString;

        public MySqlMigrationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
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

        public async Task<(bool Success, string Message)> MigrateFromCsvAsync(string filePath, string tableName)
        {
            try
            {
                // Leer el CSV
                var records = ReadCsvFile(filePath);
                if (records.Count == 0)
                {
                    return (false, "El archivo CSV está vacío.");
                }

                // Obtener columnas del CSV
                var csvColumns = records.First().Keys.ToList();
                if (csvColumns.Count == 0)
                {
                    return (false, "No se encontraron columnas en el CSV.");
                }

                // Crear la tabla automáticamente
                await DropTableIfExistsAsync(tableName);
                await CreateTableFromCsvAsync(tableName, csvColumns);

                // Insertar los datos
                await InsertRecordsToMySqlAsync(records, tableName, csvColumns);

                return (true, $"✓ Se creó la tabla '{tableName}' con {csvColumns.Count} columnas y se migraron {records.Count} registros a MySQL.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error MySQL: {ex.Message}");
            }
        }

        private async Task DropTableIfExistsAsync(string tableName)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string dropQuery = $"DROP TABLE IF EXISTS `{tableName}`";
                using (var command = new MySqlCommand(dropQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task CreateTableFromCsvAsync(string tableName, List<string> columns)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Crear tabla con columnas del CSV
                var columnDefinitions = new List<string> { "`id` INT AUTO_INCREMENT PRIMARY KEY" };

                foreach (var column in columns)
                {
                    // Limpiar nombre de columna
                    var cleanColumn = column.Trim().Replace(" ", "_").Replace("-", "_");
                    columnDefinitions.Add($"`{cleanColumn}` TEXT");
                }

                string createTableQuery = $"CREATE TABLE `{tableName}` ({string.Join(", ", columnDefinitions)}) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

                using (var createCommand = new MySqlCommand(createTableQuery, connection))
                {
                    await createCommand.ExecuteNonQueryAsync();
                }
            }
        }

        private List<Dictionary<string, object>> ReadCsvFile(string filePath)
        {
            var records = new List<Dictionary<string, object>>();

            try
            {
                // Usar encoding UTF-8 y optimizado para archivos grandes
                var encoding = System.Text.Encoding.UTF8;
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                using (var reader = new StreamReader(fileStream, encoding))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // No validar headers estrictamente
                    csv.Read();
                    csv.ReadHeader();
                    var headers = csv.HeaderRecord;

                    if (headers == null || headers.Length == 0)
                    {
                        return records;
                    }

                    // Leer registros de forma eficiente
                    while (csv.Read())
                    {
                        var record = new Dictionary<string, object>();
                        foreach (var header in headers)
                        {
                            try
                            {
                                var value = csv.GetField(header);
                                record[header] = value ?? "";
                            }
                            catch
                            {
                                record[header] = "";
                            }
                        }
                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error leyendo CSV: {ex.Message}");
            }

            return records;
        }

        private async Task InsertRecordsToMySqlAsync(List<Dictionary<string, object>> records, string tableName, List<string> csvColumns)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                if (records.Count == 0) return;

                const int batchSize = 100;
                for (int i = 0; i < records.Count; i += batchSize)
                {
                    var batch = records.Skip(i).Take(batchSize).ToList();
                    await InsertBatchAsync(connection, tableName, batch, csvColumns);
                }
            }
        }

        private async Task InsertBatchAsync(MySqlConnection connection, string tableName, 
            List<Dictionary<string, object>> batch, List<string> columns)
        {
            // Limpiar nombres de columnas
            var cleanColumns = columns.Select(c => c.Trim().Replace(" ", "_").Replace("-", "_")).ToList();
            var columnList = string.Join(", ", cleanColumns.Select(c => $"`{c}`"));
            var values = new List<string>();

            for (int i = 0; i < batch.Count; i++)
            {
                var recordValues = cleanColumns.Select((col, idx) => $"@p{i}_{idx}").ToList();
                values.Add($"({string.Join(", ", recordValues)})");
            }

            string query = $"INSERT INTO `{tableName}` ({columnList}) VALUES {string.Join(", ", values)}";

            using (var command = new MySqlCommand(query, connection))
            {
                for (int i = 0; i < batch.Count; i++)
                {
                    var record = batch[i];
                    for (int j = 0; j < columns.Count; j++)
                    {
                        var value = record[columns[j]];
                        command.Parameters.AddWithValue($"@p{i}_{j}", 
                            string.IsNullOrEmpty(value?.ToString()) ? DBNull.Value : (object)value);
                    }
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
