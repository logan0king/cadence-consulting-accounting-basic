using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using CsvHelper;
using System.Globalization;

namespace CadenceAccounting.Services
{
    public interface IReportExportService
    {
        Task<byte[]> ExportToPdfAsync(string reportData, string reportTitle = "Report");
        Task<byte[]> ExportToExcelAsync(string reportData, string reportTitle = "Report");
        Task<byte[]> ExportToCsvAsync(string reportData, string reportTitle = "Report");
        Task<byte[]> ExportToHtmlAsync(string reportData, string reportTitle = "Report");
    }

    public class ReportExportService : IReportExportService
    {
        public async Task<byte[]> ExportToPdfAsync(string reportData, string reportTitle = "Report")
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 50, 50, 25, 25);
                var writer = PdfWriter.GetInstance(document, memoryStream);
                
                document.Open();
                
                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                var title = new Paragraph(reportTitle, titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);
                
                // Add spacing
                document.Add(new Paragraph(" "));
                
                // Add report data
                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                var dataParagraph = new Paragraph(reportData, dataFont);
                document.Add(dataParagraph);
                
                document.Close();
                
                return memoryStream.ToArray();
            }
        }

        public async Task<byte[]> ExportToExcelAsync(string reportData, string reportTitle = "Report")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Report");
                
                // Add title
                worksheet.Cells[1, 1].Value = reportTitle;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1, 1, 10].Merge = true;
                
                // Add report data
                var lines = reportData.Split('\n');
                int row = 3;
                
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        worksheet.Cells[row, 1].Value = line;
                        row++;
                    }
                }
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> ExportToCsvAsync(string reportData, string reportTitle = "Report")
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Add title as first row
                csv.WriteField(reportTitle);
                csv.NextRecord();
                
                // Add empty row
                csv.WriteField("");
                csv.NextRecord();
                
                // Add report data
                var lines = reportData.Split('\n');
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        csv.WriteField(line);
                        csv.NextRecord();
                    }
                }
                
                writer.Flush();
                return memoryStream.ToArray();
            }
        }

        public async Task<byte[]> ExportToHtmlAsync(string reportData, string reportTitle = "Report")
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{reportTitle}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            line-height: 1.6;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
            border-bottom: 2px solid #333;
            padding-bottom: 10px;
        }}
        .content {{
            white-space: pre-line;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>{reportTitle}</h1>
    </div>
    <div class=""content"">{reportData}</div>
</body>
</html>";

            return System.Text.Encoding.UTF8.GetBytes(html);
        }
    }
}
