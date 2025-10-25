using CadenceAccounting.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace CadenceAccounting.Services
{
    public class ReportPdfGenerator
    {
        public async Task<byte[]> GeneratePdfAsync(ReportDefinition report, ReportData data)
        {
            using var memoryStream = new MemoryStream();
            
            // Create PDF document
            var document = new Document(PageSize.A4, 36, 36, 54, 54);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            
            document.Open();
            
            // Add report title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
            var title = new Paragraph(report.Title, titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);
            
            // Add report description if available
            if (!string.IsNullOrEmpty(report.Description))
            {
                var descFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);
                var description = new Paragraph(report.Description, descFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(description);
            }
            
            // Add generation date
            var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
            var dateText = new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingAfter = 20
            };
            document.Add(dateText);
            
            // Add data table if we have data
            if (data.Rows.Any())
            {
                await AddDataTableAsync(document, data);
            }
            else
            {
                // Add no data message
                var noDataFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.GRAY);
                var noDataText = new Paragraph("No data available for the selected criteria.", noDataFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(noDataText);
            }
            
            // Add footer
            AddFooter(document, report);
            
            document.Close();
            
            return memoryStream.ToArray();
        }

        private async Task AddDataTableAsync(Document document, ReportData data)
        {
            // Create table with number of columns
            var table = new PdfPTable(data.Columns.Count)
            {
                WidthPercentage = 100,
                SpacingBefore = 10,
                SpacingAfter = 10
            };
            
            // Set column widths (equal distribution)
            var columnWidths = new float[data.Columns.Count];
            for (int i = 0; i < data.Columns.Count; i++)
            {
                columnWidths[i] = 100f / data.Columns.Count;
            }
            table.SetWidths(columnWidths);
            
            // Add header row
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
            var headerCellFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
            
            foreach (var column in data.Columns)
            {
                var cell = new PdfPCell(new Phrase(column.Name, headerCellFont))
                {
                    BackgroundColor = new BaseColor(52, 73, 94), // Dark blue-gray
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8
                };
                table.AddCell(cell);
            }
            
            // Add data rows
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
            
            foreach (var row in data.Rows.Take(1000)) // Limit to 1000 rows for performance
            {
                foreach (var column in data.Columns)
                {
                    var value = row.ContainsKey(column.Name) ? row[column.Name] : "";
                    var displayValue = FormatCellValue(value);
                    
                    var cell = new PdfPCell(new Phrase(displayValue, dataFont))
                    {
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 6
                    };
                    table.AddCell(cell);
                }
            }
            
            document.Add(table);
            
            // Add row count summary
            if (data.TotalRows > 1000)
            {
                var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
                var summaryText = new Paragraph($"Showing first 1,000 of {data.TotalRows:N0} rows", summaryFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 10
                };
                document.Add(summaryText);
            }
        }

        private string FormatCellValue(object? value)
        {
            if (value == null || value == DBNull.Value)
                return "";
            
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (value is decimal || value is double || value is float)
                return string.Format("{0:N2}", value);
            
            return value.ToString() ?? "";
        }

        private void AddFooter(Document document, ReportDefinition report)
        {
            try
            {
                var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
                var footerText = new Paragraph($"Report: {report.Title} | Generated by Cadence Accounting", footerFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20
                };
                document.Add(footerText);
            }
            catch
            {
                // Ignore footer errors
            }
        }
    }
}
