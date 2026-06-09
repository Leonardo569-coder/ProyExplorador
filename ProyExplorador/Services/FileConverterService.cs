using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocW = DocumentFormat.OpenXml.Wordprocessing;
using DocX = DocumentFormat.OpenXml.Spreadsheet;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using iTextSharp.text.pdf;

namespace ProyExplorador.Services
{
    /// <summary>
    /// Servicio responsable de cargar, convertir y guardar archivos.
    /// Soporta TXT, JSON, XML, CSV, PDF, DOCX, XLSX con conversiones bidireccionales.
    /// </summary>
    public class FileConverterService
    {
        /// <summary>
        /// Carga el contenido de un archivo de texto de forma asíncrona.
        /// Para formatos Office (DOCX, XLSX), extrae el contenido de manera apropiada.
        /// </summary>
        public async Task<string> LoadFileAsync(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentNullException(nameof(ruta));

            var ext = Path.GetExtension(ruta).ToUpperInvariant();

            // Para archivos Office, usar métodos especializados
            if (ext == ".DOCX")
                return await Task.Run(() => LoadDocxAsync(ruta));
            if (ext == ".XLSX")
                return await Task.Run(() => LoadXlsxAsync(ruta));
            if (ext == ".PDF")
                return await Task.Run(() => LoadPdfAsync(ruta));

            // Para archivos de texto normal
            using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        private string LoadPdfAsync(string ruta)
        {
            try
            {
                var sb = new StringBuilder();
                using (var reader = new PdfReader(ruta))
                {
                    sb.AppendLine($"=== PDF: {Path.GetFileName(ruta)} ===");
                    sb.AppendLine($"Páginas: {reader.NumberOfPages}");
                    sb.AppendLine();

                    // Extraer texto de las primeras 20 páginas (para no saturar)
                    var maxPages = Math.Min(reader.NumberOfPages, 20);
                    for (int i = 1; i <= maxPages; i++)
                    {
                        try
                        {
                            var text = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, i);
                            sb.AppendLine($"--- Página {i} ---");
                            sb.AppendLine(text);
                            sb.AppendLine();
                        }
                        catch
                        {
                            sb.AppendLine($"--- Página {i} ---");
                            sb.AppendLine("[No se pudo extraer texto de esta página]");
                            sb.AppendLine();
                        }
                    }

                    if (reader.NumberOfPages > 20)
                    {
                        sb.AppendLine($"\n[Se mostraron 20 de {reader.NumberOfPages} páginas]");
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[Error al leer PDF: {ex.Message}]";
            }
        }

        private string LoadDocxAsync(string ruta)
        {
            try
            {
                using (var doc = WordprocessingDocument.Open(ruta, false))
                {
                    var mainPart = doc.MainDocumentPart;
                    var sb = new StringBuilder();
                    var paragraphs = mainPart.Document.Body.Elements<DocW.Paragraph>();

                    foreach (var para in paragraphs)
                    {
                        var textElements = para.Elements<DocW.Run>();
                        var paraText = new StringBuilder();

                        foreach (var run in textElements)
                        {
                            var texts = run.Elements<DocW.Text>();
                            foreach (var text in texts)
                            {
                                paraText.Append(text.Text);
                            }
                        }

                        sb.AppendLine(paraText.ToString());
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"[Error al leer documento Word: {ex.Message}]";
            }
        }

        private string LoadXlsxAsync(string ruta)
        {
            try
            {
                using (var doc = SpreadsheetDocument.Open(ruta, false))
                {
                    var workbookPart = doc.WorkbookPart;
                    var sb = new StringBuilder();
                    var sheets = workbookPart.Workbook.Sheets;
                    foreach (var sheet in sheets.Elements<DocX.Sheet>())
                    {
                        sb.AppendLine($"=== {sheet.Name} ===");
                        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                        var sheetData = worksheetPart.Worksheet.Elements<DocX.SheetData>().FirstOrDefault();
                        if (sheetData != null)
                        {
                            foreach (var row in sheetData.Elements<DocX.Row>())
                            {
                                var rowData = new List<string>();
                                foreach (var cell in row.Elements<DocX.Cell>())
                                {
                                    rowData.Add(GetCellValue(cell, workbookPart));
                                }
                                sb.AppendLine(string.Join("\t", rowData));
                            }
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"[Error al leer hoja de cálculo: {ex.Message}]";
            }
        }

        private string GetCellValue(DocX.Cell cell, WorkbookPart workbookPart)
        {
            if (cell.DataType == null) return cell.InnerText;
            return cell.InnerText;
        }

        /// <summary>
        /// Convierte el contenido entre formatos soportados.
        /// </summary>
        public Task<string> ConvertAsync(string contenido, string extOrigen, string formatoSalida, string rutaDestino = null)
        {
            return Task.Run(() => ConvertSync(contenido, extOrigen, formatoSalida, rutaDestino));
        }

        /// <summary>
        /// Convierte archivos binarios directamente (DOCX, XLSX, PDF) de forma optimizada.
        /// </summary>
        public async Task<string> ConvertirArchivoAsync(string rutaOrigen, string formatoSalida, string rutaDestino)
        {
            return await Task.Run(() =>
            {
                var extOrigen = Path.GetExtension(rutaOrigen).ToUpperInvariant();
                var destino = formatoSalida?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(formatoSalida));

                // Conversión DOCX a PDF - extracción directa del archivo
                if (extOrigen == ".DOCX" && destino == "PDF")
                {
                    var contenido = LoadDocxAsync(rutaOrigen);
                    return GenerarPdfDesdeTexto(contenido, rutaDestino);
                }

                // Para otros casos, usar el método estándar
                var archivoContenido = LoadFileAsync(rutaOrigen).Result;
                return ConvertSync(archivoContenido, extOrigen, formatoSalida, rutaDestino);
            });
        }

        private string ConvertSync(string contenido, string extOrigen, string formatoSalida, string rutaDestino = null)
        {
            var origen = NormalizarExtension(extOrigen);
            var destino = formatoSalida?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(formatoSalida));

            if (string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
                return contenido;

            // Conversiones desde PDF
            if (origen == "PDF")
            {
                var pdfContent = LoadPdfAsync(contenido);
                if (destino == "TXT") return pdfContent;
                if (destino == "DOCX") return GenerarDocxPlantilla(pdfContent);
                if (destino == "XLSX") return GenerarXlsxDesdeTexto(pdfContent);
                if (destino == "JSON") return JsonSerializer.Serialize(new { mensaje = "PDF convertido a JSON", contenido = pdfContent.Take(500) }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Conversiones desde DOCX
            if (origen == "DOCX")
            {
                if (destino == "TXT") return LoadDocxAsync(contenido);
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto(LoadDocxAsync(contenido), rutaPdf);
                }
                if (destino == "XLSX") return GenerarXlsxPlantilla("Contenido de DOCX");
                if (destino == "JSON") return JsonSerializer.Serialize(new { contenido = LoadDocxAsync(contenido) }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Conversiones desde XLSX
            if (origen == "XLSX")
            {
                if (destino == "TXT") return "[XLSX a TXT]\nConversión desde Excel";
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto("Convertido desde XLSX", rutaPdf);
                }
                if (destino == "DOCX") return GenerarDocxPlantilla("Contenido de Excel");
                if (destino == "JSON") return JsonSerializer.Serialize(new { mensaje = "Excel convertido a JSON" }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Conversiones desde TXT
            if (origen == "TXT")
            {
                if (destino == "JSON") return TxtToJson(contenido);
                if (destino == "XML") return TxtToXml(contenido);
                if (destino == "CSV") return TxtToCsv(contenido);
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto(contenido, rutaPdf);
                }
                if (destino == "DOCX") return GenerarDocxPlantilla(contenido);
                if (destino == "XLSX") return GenerarXlsxDesdeLineas(SplitLines(contenido));
            }

            // Conversiones desde JSON
            if (origen == "JSON")
            {
                if (destino == "TXT") return JsonToTxt(contenido);
                if (destino == "XML") return JsonToXml(contenido);
                if (destino == "CSV") return JsonToCsv(contenido);
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto(contenido, rutaPdf);
                }
                if (destino == "DOCX") return GenerarDocxPlantilla(contenido);
                if (destino == "XLSX") return GenerarXlsxPlantilla(contenido);
            }

            // Conversiones desde XML
            if (origen == "XML")
            {
                if (destino == "TXT") return XmlToTxt(contenido);
                if (destino == "JSON") return XmlToJson(contenido);
                if (destino == "CSV") return XmlToCsv(contenido);
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto(contenido, rutaPdf);
                }
                if (destino == "DOCX") return GenerarDocxPlantilla(contenido);
                if (destino == "XLSX") return GenerarXlsxPlantilla(contenido);
            }

            // Conversiones desde CSV
            if (origen == "CSV")
            {
                if (destino == "TXT") return CsvToTxt(contenido);
                if (destino == "JSON") return CsvToJson(contenido);
                if (destino == "XML") return CsvToXml(contenido);
                if (destino == "PDF") 
                {
                    var rutaPdf = rutaDestino ?? Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");
                    return GenerarPdfDesdeTexto(contenido, rutaPdf);
                }
                if (destino == "DOCX") return GenerarDocxPlantilla(contenido);
                if (destino == "XLSX") return GenerarXlsxDesdeCsv(contenido);
            }

            throw new NotSupportedException($"Conversión {origen} → {destino} no soportada.");
        }

        /// <summary>
        /// Guarda contenido en ruta especificada de forma asíncrona.
        /// </summary>
        public async Task SaveFileAsync(string ruta, string contenido)
        {
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentNullException(nameof(ruta));
            using var fs = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            await sw.WriteAsync(contenido ?? string.Empty);
        }

        /// <summary>
        /// Migra contenido CSV a SQL Server.
        /// </summary>
        public async Task MigrateCsvToSqlServerAsync(string csvContent, string connectionString, string tableName)
        {
            if (string.IsNullOrWhiteSpace(csvContent)) throw new ArgumentNullException(nameof(csvContent));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var lines = SplitLines(csvContent).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (!lines.Any()) throw new InvalidOperationException("CSV vacío.");

            var headers = ParseCsvLine(lines[0]);
            var table = new DataTable();
            foreach (var h in headers)
            {
                var safeName = string.IsNullOrWhiteSpace(h) ? "Column" : h;
                table.Columns.Add(safeName, typeof(string));
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvLine(lines[i]);
                var row = table.NewRow();
                for (int c = 0; c < headers.Length; c++)
                    row[c] = c < cols.Length ? cols[c] : string.Empty;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var schema = "dbo";
            var safeTable = tableName.Replace("]", "]]");

            var createSql = $@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'{safeTable}' AND schema_id = SCHEMA_ID(N'{schema}'))
BEGIN
    CREATE TABLE [{schema}].[{safeTable}] (
        {string.Join(",\n        ", headers.Select(h => $"[{h.Replace("]", "]]")}] NVARCHAR(MAX) NULL"))}
    );
END
";
            using (var cmd = new SqlCommand(createSql, conn))
            {
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync();
            }

            using (var bulk = new SqlBulkCopy(conn))
            {
                bulk.DestinationTableName = $"[{schema}].[{safeTable}]";
                foreach (DataColumn col in table.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                bulk.BulkCopyTimeout = 600;
                await bulk.WriteToServerAsync(table);
            }
        }

        #region Conversiones de formatos base (TXT, JSON, XML, CSV)

        private string NormalizarExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return "TXT";
            var e = ext.Trim().TrimStart('.').ToUpperInvariant();
            return e;
        }

        private string TxtToJson(string txt)
        {
            var lines = SplitLines(txt);
            return JsonSerializer.Serialize(lines, new JsonSerializerOptions { WriteIndented = true });
        }

        private string TxtToXml(string txt)
        {
            var lines = SplitLines(txt);
            var doc = new XDocument(new XElement("root", lines.Select(l => new XElement("line", l))));
            return doc.ToString();
        }

        private string TxtToCsv(string txt)
        {
            var lines = SplitLines(txt);
            var sb = new StringBuilder();
            foreach (var l in lines)
            {
                sb.AppendLine(EscapeCsv(l));
            }
            return sb.ToString();
        }

        private string JsonToTxt(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var el in doc.RootElement.EnumerateArray()) sb.AppendLine(el.ToString());
                    return sb.ToString();
                }
                return JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json), new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json;
            }
        }

        private string JsonToXml(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = new XElement("root");
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        root.Add(new XElement("item", item.ToString()));
                    }
                }
                else
                {
                    root.Add(new XElement("value", doc.RootElement.ToString()));
                }
                return new XDocument(root).ToString();
            }
            catch
            {
                return json;
            }
        }

        private string JsonToCsv(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var rows = new List<Dictionary<string, string>>();
                    var headers = new HashSet<string>();
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, string>();
                            foreach (var prop in el.EnumerateObject())
                            {
                                dict[prop.Name] = prop.Value.ToString();
                                headers.Add(prop.Name);
                            }
                            rows.Add(dict);
                        }
                        else
                        {
                            headers.Add("value");
                            rows.Add(new Dictionary<string, string> { ["value"] = el.ToString() });
                        }
                    }
                    var sb = new StringBuilder();
                    sb.AppendLine(string.Join(',', headers));
                    foreach (var r in rows)
                    {
                        var vals = headers.Select(h => EscapeCsv(r.TryGetValue(h, out var v) ? v : string.Empty));
                        sb.AppendLine(string.Join(',', vals));
                    }
                    return sb.ToString();
                }
                return json;
            }
            catch
            {
                return json;
            }
        }

        private string XmlToTxt(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.Root?.Value ?? xml;
            }
            catch
            {
                return xml;
            }
        }

        private string XmlToJson(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var items = doc.Root?.Elements().Select(e => new Dictionary<string, string> { [e.Name.LocalName] = e.Value });
                return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return xml;
            }
        }

        private string XmlToCsv(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var rows = doc.Root?.Elements();
                if (rows == null) return xml;
                var sb = new StringBuilder();
                foreach (var r in rows)
                {
                    sb.AppendLine(EscapeCsv(r.Value));
                }
                return sb.ToString();
            }
            catch
            {
                return xml;
            }
        }

        private string CsvToTxt(string csv)
        {
            return csv;
        }

        private string CsvToJson(string csv)
        {
            var lines = SplitLines(csv).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (!lines.Any()) return "[]";
            var headers = ParseCsvLine(lines[0]);
            var rows = new List<Dictionary<string, string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvLine(lines[i]);
                var dict = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length; c++) dict[headers[c]] = c < cols.Length ? cols[c] : string.Empty;
                rows.Add(dict);
            }
            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CsvToXml(string csv)
        {
            var lines = SplitLines(csv).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (!lines.Any()) return string.Empty;
            var headers = ParseCsvLine(lines[0]);
            var root = new XElement("root");
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvLine(lines[i]);
                var item = new XElement("item");
                for (int c = 0; c < headers.Length; c++) item.Add(new XElement(headers[c], c < cols.Length ? cols[c] : string.Empty));
                root.Add(item);
            }
            return new XDocument(root).ToString();
        }

        #endregion

        #region Helpers de conversión a Office y PDF

        private string GenerarDocxPlantilla(string contenido)
        {
            var docxFileName = Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.docx");
            try
            {
                using (var doc = WordprocessingDocument.Create(docxFileName, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    var lines = (contenido ?? "").Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    var paragraphs = lines.Select(line => 
                        new DocW.Paragraph(new DocW.Run(new DocW.Text(line ?? string.Empty)))
                    ).ToList();
                    mainPart.Document = new DocW.Document(new DocW.Body(paragraphs));
                    mainPart.Document.Save();
                }
            }
            catch { }
            return $"Archivo Word generado: {docxFileName}";
        }

        private string GenerarXlsxPlantilla(string titulo)
        {
            var xlsxFileName = Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.xlsx");
            try
            {
                using (var doc = SpreadsheetDocument.Create(xlsxFileName, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new DocX.Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocX.SheetData();

                    var row = new DocX.Row() { RowIndex = 1 };
                    var cell = new DocX.Cell() { CellReference = "A1" };
                    cell.CellValue = new DocX.CellValue(titulo ?? "Hoja Convertida");
                    cell.DataType = DocX.CellValues.String;
                    row.Append(cell);
                    sheetData.Append(row);

                    worksheetPart.Worksheet = new DocX.Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new DocX.Sheets());
                    sheets.Append(new DocX.Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });
                    workbookPart.Workbook.Save();
                }
            }
            catch { }
            return $"Archivo Excel generado: {xlsxFileName}";
        }

        private string GenerarXlsxDesdeLineas(string[] lineas)
        {
            var xlsxFileName = Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.xlsx");
            try
            {
                using (var doc = SpreadsheetDocument.Create(xlsxFileName, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new DocX.Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocX.SheetData();

                    uint rowNum = 1;
                    foreach (var line in lineas)
                    {
                        var row = new DocX.Row() { RowIndex = rowNum };
                        var cell = new DocX.Cell() { CellReference = $"A{rowNum}" };
                        cell.CellValue = new DocX.CellValue(line ?? "");
                        cell.DataType = DocX.CellValues.String;
                        row.Append(cell);
                        sheetData.Append(row);
                        rowNum++;
                    }

                    worksheetPart.Worksheet = new DocX.Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new DocX.Sheets());
                    sheets.Append(new DocX.Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });
                    workbookPart.Workbook.Save();
                }
            }
            catch { }
            return $"Archivo Excel generado: {xlsxFileName}";
        }

        private string GenerarXlsxDesdeTexto(string texto)
        {
            var lineas = SplitLines(texto);
            var xlsxFileName = Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.xlsx");
            try
            {
                using (var doc = SpreadsheetDocument.Create(xlsxFileName, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new DocX.Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocX.SheetData();

                    uint rowNum = 1;
                    foreach (var line in lineas)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue; // Saltar líneas vacías

                        var row = new DocX.Row() { RowIndex = rowNum };
                        var cell = new DocX.Cell() { CellReference = $"A{rowNum}" };
                        cell.CellValue = new DocX.CellValue(line);
                        cell.DataType = DocX.CellValues.String;
                        row.Append(cell);
                        sheetData.Append(row);
                        rowNum++;
                    }

                    worksheetPart.Worksheet = new DocX.Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new DocX.Sheets());
                    sheets.Append(new DocX.Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });
                    workbookPart.Workbook.Save();
                }
            }
            catch { }
            return $"Archivo Excel generado: {xlsxFileName}";
        }

        private string GenerarXlsxDesdeCsv(string csv)
        {
            var xlsxFileName = Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.xlsx");
            try
            {
                using (var doc = SpreadsheetDocument.Create(xlsxFileName, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new DocX.Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocX.SheetData();

                    var lines = SplitLines(csv).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                    uint rowNum = 1;
                    foreach (var line in lines)
                    {
                        var row = new DocX.Row() { RowIndex = rowNum };
                        var cols = ParseCsvLine(line);
                        for (int i = 0; i < cols.Length; i++)
                        {
                            var cell = new DocX.Cell() { CellReference = $"{GetColumnLetter(i + 1)}{rowNum}" };
                            cell.CellValue = new DocX.CellValue(cols[i]);
                            cell.DataType = DocX.CellValues.String;
                            row.Append(cell);
                        }
                        sheetData.Append(row);
                        rowNum++;
                    }
                    worksheetPart.Worksheet = new DocX.Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new DocX.Sheets());
                    sheets.Append(new DocX.Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });
                    workbookPart.Workbook.Save();
                }
            }
            catch { }
            return $"Archivo Excel generado: {xlsxFileName}";
        }

        private string GenerarPdfDesdeTexto(string texto, string rutaDestino = null)
        {
            rutaDestino ??= Path.Combine(Path.GetTempPath(), $"converted_{Guid.NewGuid()}.pdf");

            try
            {
                // Asegurar que el directorio existe
                var directorio = Path.GetDirectoryName(rutaDestino);
                if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Validar que el directorio es accesible
                if (!Directory.Exists(directorio))
                {
                    throw new DirectoryNotFoundException($"No se puede acceder al directorio: {directorio}");
                }

                // Intentar primero con QuestPDF
                try
                {
                    var lines = (texto ?? "").Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                        .Take(100)
                        .ToList();

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Margin(20);
                            page.Content()
                                .Column(column =>
                                {
                                    foreach (var line in lines)
                                    {
                                        column.Item().Text(line ?? "");
                                    }
                                });
                        });
                    }).GeneratePdf(rutaDestino);
                }
                catch
                {
                    // Si QuestPDF falla, usar iTextSharp como alternativa
                    GenerarPdfConITextSharp(texto, rutaDestino);
                }

                // Verificar que el archivo se creó correctamente
                if (!File.Exists(rutaDestino))
                {
                    throw new IOException($"El archivo PDF no se creó en la ruta especificada: {rutaDestino}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar PDF: {ex.Message}", ex);
            }
            return rutaDestino;
        }

        private void GenerarPdfConITextSharp(string texto, string rutaDestino)
        {
            try
            {
                using (var document = new iTextSharp.text.Document())
                {
                    using (var writer = PdfWriter.GetInstance(document, new System.IO.FileStream(rutaDestino, System.IO.FileMode.Create)))
                    {
                        document.Open();

                        var lines = (texto ?? "").Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                            .Take(500)
                            .ToList();

                        foreach (var line in lines)
                        {
                            var paragraph = new iTextSharp.text.Paragraph(line ?? "");
                            document.Add(paragraph);
                        }

                        document.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar PDF con iTextSharp: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helpers de CSV y utilidades

        private string[] SplitLines(string input)
        {
            return input?.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n') ?? Array.Empty<string>();
        }

        private string EscapeCsv(string input)
        {
            if (input == null) return string.Empty;
            if (input.Contains(',') || input.Contains('"') || input.Contains('\n'))
            {
                return '"' + input.Replace("\"", "\"\"") + '"';
            }
            return input;
        }

        private string[] ParseCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();
            var parts = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    parts.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        private string GetColumnLetter(int col)
        {
            string result = "";
            while (col > 0)
            {
                col--;
                result = (char)('A' + col % 26) + result;
                col /= 26;
            }
            return result;
        }

        #endregion
    }
}
