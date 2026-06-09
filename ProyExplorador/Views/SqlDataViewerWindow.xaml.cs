using ProyExplorador.Services;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;

namespace ProyExplorador.Views
{
    public partial class SqlDataViewerWindow : Window
    {
        private readonly SqlDataService _sqlService;
        private DataTable _currentData;

        public SqlDataViewerWindow(string connectionString)
        {
            InitializeComponent();
            _sqlService = new SqlDataService(connectionString);
            _currentData = new DataTable();
        }

        private async void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tableName = TxtTableName.Text.Trim();
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    MessageBox.Show(this, "Ingresa el nombre de la tabla.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TxtStatus.Text = $"Cargando tabla '{tableName}'...";
                _currentData = await _sqlService.GetTableAsync(tableName);
                DataGridResults.ItemsSource = _currentData.DefaultView;
                TxtRecordCount.Text = $"Registros: {_currentData.Rows.Count}";
                TxtStatus.Text = $"Tabla '{tableName}' cargada exitosamente";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Error al cargar datos";
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tableName = TxtTableName.Text.Trim();
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    MessageBox.Show(this, "Ingresa el nombre de la tabla.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TxtStatus.Text = "Actualizando datos...";
                _currentData = await _sqlService.GetTableAsync(tableName);
                DataGridResults.ItemsSource = _currentData.DefaultView;
                TxtRecordCount.Text = $"Registros: {_currentData.Rows.Count}";
                TxtStatus.Text = "Datos actualizados";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Error al actualizar datos";
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentData.Rows.Count == 0)
                {
                    MessageBox.Show(this, "No hay datos para exportar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV (*.csv)|*.csv",
                    FileName = $"{TxtTableName.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dlg.ShowDialog() != true) return;

                ExportToCsv(_currentData, dlg.FileName);
                MessageBox.Show(this, $"Datos exportados a:\n{dlg.FileName}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatus.Text = "Datos exportados exitosamente";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(DataTable table, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Escribir encabezados
                var headers = new StringBuilder();
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (i > 0) headers.Append(",");
                    headers.Append($"\"{table.Columns[i].ColumnName}\"");
                }
                writer.WriteLine(headers.ToString());

                // Escribir datos
                foreach (DataRow row in table.Rows)
                {
                    var values = new StringBuilder();
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        if (i > 0) values.Append(",");
                        var value = row[i]?.ToString() ?? "";
                        values.Append($"\"{value.Replace("\"", "\"\"")}\"");
                    }
                    writer.WriteLine(values.ToString());
                }
            }
        }
    }
}
