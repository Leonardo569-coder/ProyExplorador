using System;
using System.Windows;
using System.Windows.Controls;
using ProyExplorador.Services;
using Microsoft.Extensions.Configuration;

namespace ProyExplorador.Views
{
    /// <summary>
    /// Lógica de interacción para SqlMigrationWindow.xaml
    /// </summary>
    public partial class SqlMigrationWindow : Window
    {
        private IDataBaseMigration _migrationService;
        private bool _isInitializing = false;

        public string ConnectionString => TxtConnectionString.Text;
        public string TableName => TxtTableName.Text;

        public SqlMigrationWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _isInitializing = true;

            // Agregar los manejadores después de que todo esté listo
            RdoSqlServer.Checked += RdoDatabase_Checked;
            RdoMySQL.Checked += RdoDatabase_Checked;

            InitializeSqlServer();
            _isInitializing = false;
        }

        private void InitializeSqlServer()
        {
            try
            {
                var sqlServerConnection = App.Configuration.GetConnectionString("SqlServerConnection");
                TxtConnectionString.Text = sqlServerConnection;
                _migrationService = new SqlServerMigrationService(sqlServerConnection);
                UpdateInfo("SQL Server (LocalDB)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeMySQL()
        {
            try
            {
                var mySqlConnection = App.Configuration.GetConnectionString("MySqlConnection");
                TxtConnectionString.Text = mySqlConnection;
                _migrationService = new MySqlMigrationService(mySqlConnection);
                UpdateInfo("MySQL (HeidiSQL)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RdoDatabase_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            // Verificar que los controles existan antes de usarlos
            if (LblConnection == null || RdoSqlServer == null || RdoMySQL == null)
                return;

            try
            {
                if (RdoSqlServer.IsChecked == true)
                {
                    LblConnection.Text = "Cadena de conexión (SQL Server):";
                    InitializeSqlServer();
                }
                else if (RdoMySQL.IsChecked == true)
                {
                    LblConnection.Text = "Cadena de conexión (MySQL):";
                    InitializeMySQL();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_migrationService == null)
            {
                MessageBox.Show("Por favor, selecciona una base de datos primero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool connected = await _migrationService.TestConnectionAsync();

            if (connected)
            {
                MessageBox.Show("✓ Conexión exitosa", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LblInfo.Foreground = System.Windows.Media.Brushes.Green;
                LblInfo.Text = "✓ Conexión verificada correctamente";
            }
            else
            {
                MessageBox.Show("❌ No se pudo conectar. Verifica la cadena de conexión.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LblInfo.Foreground = System.Windows.Media.Brushes.Red;
                LblInfo.Text = "❌ Error de conexión";
            }
        }

        private async void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                MessageBox.Show(this, "Introduzca la cadena de conexión.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TableName))
            {
                MessageBox.Show(this, "Introduzca el nombre de la tabla.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Abrir diálogo para seleccionar archivo
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar archivo para migrar"
            };

            if (dialog.ShowDialog() == true)
            {
                // Deshabilitar botones mientras se realiza la migración
                BtnOk.IsEnabled = false;
                BtnCancel.IsEnabled = false;
                LblInfo.Text = "⏳ Migrando datos... Por favor espera...";
                LblInfo.Foreground = System.Windows.Media.Brushes.Orange;

                try
                {
                    _migrationService = RdoSqlServer.IsChecked == true 
                        ? new SqlServerMigrationService(ConnectionString) as IDataBaseMigration
                        : new MySqlMigrationService(ConnectionString) as IDataBaseMigration;

                    // Ejecutar migración directamente sin Task.Run
                    // ConfigureAwait(false) para evitar deadlocks en WPF
                    var (success, message) = await _migrationService.MigrateFromCsvAsync(dialog.FileName, TableName).ConfigureAwait(false);

                    // Volver al thread principal para actualizar UI
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(message, 
                            success ? "✓ Éxito" : "❌ Error", 
                            MessageBoxButton.OK, 
                            success ? MessageBoxImage.Information : MessageBoxImage.Error);

                        if (success)
                        {
                            LblInfo.Text = "✓ Migración completada exitosamente";
                            LblInfo.Foreground = System.Windows.Media.Brushes.Green;
                            Close();
                        }
                        else
                        {
                            LblInfo.Text = "❌ Error en la migración";
                            LblInfo.Foreground = System.Windows.Media.Brushes.Red;
                            BtnOk.IsEnabled = true;
                            BtnCancel.IsEnabled = true;
                        }
                    });
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Error: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMsg += $"\n\nDetalles:\n{ex.InnerException.Message}";
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        LblInfo.Text = "❌ Error en la migración";
                        LblInfo.Foreground = System.Windows.Media.Brushes.Red;
                        BtnOk.IsEnabled = true;
                        BtnCancel.IsEnabled = true;
                    });
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateInfo(string dbType)
        {
            LblInfo.Text = $"Base de datos actual: {dbType}";
            LblInfo.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void RdoMySQL_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
