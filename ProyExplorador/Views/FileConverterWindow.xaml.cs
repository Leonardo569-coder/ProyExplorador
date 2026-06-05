using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ProyExplorador.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ProyExplorador.Views
{
    public partial class FileConverterWindow : Window
    {
        private readonly FileConverterService _servicio;
        private string _contenidoOriginal = string.Empty;

        public FileConverterWindow()
        {
            InitializeComponent();
            _servicio = new FileConverterService();
            CmbFormatoSalida.SelectedIndex = 0;
        }

        private async void BtnExaminar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Todos los archivos|*.*|Documentos|*.txt;*.json;*.xml;*.csv";
                if (dlg.ShowDialog(this) == true)
                {
                    TxtRutaArchivo.Text = dlg.FileName;
                    await CargarArchivoAsync(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error al abrir el archivo: {ex.Message}");
            }
        }

        private async Task CargarArchivoAsync(string ruta)
        {
            try
            {
                MostrarEstado("Cargando...");
                _contenidoOriginal = await _servicio.LoadFileAsync(ruta);
                PreviewTextBox.Text = _contenidoOriginal;
                MostrarEstado("Archivo cargado");
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error al cargar: {ex.Message}");
            }
        }

        private async void BtnConvertir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtRutaArchivo.Text))
                {
                    MostrarEstado("Seleccione un archivo primero.");
                    return;
                }

                var formatoSalida = ((System.Windows.Controls.ComboBoxItem)CmbFormatoSalida.SelectedItem).Tag.ToString();
                MostrarEstado("Convirtiendo...");
                var resultado = await _servicio.ConvertAsync(_contenidoOriginal, Path.GetExtension(TxtRutaArchivo.Text), formatoSalida);
                PreviewTextBox.Text = resultado;
                MostrarEstado("Conversión completada");
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error al convertir: {ex.Message}");
            }
        }

        private async void BtnGuardarComo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "TXT (*.txt)|*.txt|JSON (*.json)|*.json|XML (*.xml)|*.xml|CSV (*.csv)|*.csv";
                if (dlg.ShowDialog(this) == true)
                {
                    MostrarEstado("Guardando...");
                    await _servicio.SaveFileAsync(dlg.FileName, PreviewTextBox.Text);
                    MostrarEstado("Archivo guardado");
                }
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error al guardar: {ex.Message}");
            }
        }

        private async void BtnMigrarSql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SqlMigrationWindow { Owner = this };
                if (dlg.ShowDialog() != true) return;

                var conn = dlg.ConnectionString;
                var table = dlg.TableName;

                MostrarEstado("Preparando migración...");
                var formatoSalida = ((System.Windows.Controls.ComboBoxItem)CmbFormatoSalida.SelectedItem).Tag.ToString();
                string csvContent;
                if (formatoSalida.Equals("CSV", StringComparison.OrdinalIgnoreCase))
                    csvContent = PreviewTextBox.Text;
                else
                    csvContent = await _servicio.ConvertAsync(_contenidoOriginal, Path.GetExtension(TxtRutaArchivo.Text), "CSV");

                MostrarEstado("Migrando a SQL Server...");
                await _servicio.MigrateCsvToSqlServerAsync(csvContent, conn, table);
                MostrarEstado("Migración completada");
                MessageBox.Show(this, "Migración finalizada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error en migración: {ex.Message}");
                MessageBox.Show(this, $"Error durante la migración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarEstado(string mensaje)
        {
            StatusTextBlock.Text = mensaje;
        }
    }
}