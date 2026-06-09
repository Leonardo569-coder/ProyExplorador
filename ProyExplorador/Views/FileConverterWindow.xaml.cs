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
        private string _rutaArchivoOriginal = string.Empty;

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
                dlg.Filter = "Todos los archivos|*.*|Documentos|*.txt;*.json;*.xml;*.csv;*.pdf;*.docx;*.xlsx|Archivos PDF|*.pdf|Documentos Word|*.docx|Hojas de Cálculo|*.xlsx";
                if (dlg.ShowDialog(this) == true)
                {
                    TxtRutaArchivo.Text = dlg.FileName;
                    _rutaArchivoOriginal = dlg.FileName;
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

                // Si es conversión a PDF o DOCX o XLSX, mostrar diálogo de guardar
                if (formatoSalida.Equals("PDF", StringComparison.OrdinalIgnoreCase) ||
                    formatoSalida.Equals("DOCX", StringComparison.OrdinalIgnoreCase) ||
                    formatoSalida.Equals("XLSX", StringComparison.OrdinalIgnoreCase))
                {
                    var dlg = new SaveFileDialog();
                    dlg.Filter = formatoSalida switch
                    {
                        "PDF" => "Archivos PDF|*.pdf",
                        "DOCX" => "Documentos Word|*.docx",
                        "XLSX" => "Hojas de Cálculo|*.xlsx",
                        _ => "Todos los archivos|*.*"
                    };
                    dlg.FileName = Path.GetFileNameWithoutExtension(TxtRutaArchivo.Text) + "_convertido";
                    dlg.DefaultExt = formatoSalida.ToLower();

                    if (dlg.ShowDialog(this) != true)
                    {
                        MostrarEstado("Conversión cancelada");
                        return;
                    }

                    // Asegurar que el archivo tiene la extensión correcta
                    var rutaFinal = dlg.FileName;
                    if (!rutaFinal.EndsWith("." + formatoSalida.ToLower()))
                    {
                        rutaFinal = Path.GetFileNameWithoutExtension(rutaFinal) + "." + formatoSalida.ToLower();
                        rutaFinal = Path.Combine(Path.GetDirectoryName(dlg.FileName) ?? "", rutaFinal);
                    }

                    var resultado = await _servicio.ConvertAsync(_contenidoOriginal, Path.GetExtension(_rutaArchivoOriginal), formatoSalida, rutaFinal);

                    // Para PDF, mostrar mensaje informativo; para otros formatos, mostrar contenido
                    if (formatoSalida.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                    {
                        PreviewTextBox.Text = $"PDF generado exitosamente en:\n{rutaFinal}";
                    }
                    else
                    {
                        PreviewTextBox.Text = resultado;
                    }

                    MostrarEstado($"Conversión completada. Archivo guardado: {rutaFinal}");

                    // Verificar que el archivo existe antes de ofrecer abrirlo
                    if (!File.Exists(rutaFinal))
                    {
                        MessageBox.Show(this, $"Error: El archivo no se guardó correctamente en:\n{rutaFinal}\n\nVerifica que la ruta es válida y tienes permisos de escritura.", "Error al guardar", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Ofrecer abrir el archivo
                    var respuesta = MessageBox.Show(this, 
                        $"Archivo guardado en:\n{rutaFinal}\n\n¿Deseas abrir el archivo?", 
                        "Conversión Completada", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Information);

                    if (respuesta == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = rutaFinal,
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(rutaFinal) ?? ""
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, $"No se pudo abrir el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // Para formatos de texto, mostrar en preview
                    var resultado = await _servicio.ConvertAsync(_contenidoOriginal, Path.GetExtension(TxtRutaArchivo.Text), formatoSalida);
                    PreviewTextBox.Text = resultado;
                    MostrarEstado("Conversión completada");
                }
            }
            catch (Exception ex)
            {
                MostrarEstado($"Error al convertir: {ex.Message}");
                MessageBox.Show(this, $"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardarComo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "TXT (*.txt)|*.txt|JSON (*.json)|*.json|XML (*.xml)|*.xml|CSV (*.csv)|*.csv|PDF (*.pdf)|*.pdf|Word (*.docx)|*.docx|Excel (*.xlsx)|*.xlsx";
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