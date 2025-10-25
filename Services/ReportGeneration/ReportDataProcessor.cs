using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CadenceAccounting.Models;

namespace CadenceAccounting.Services.ReportGeneration
{
    public class ReportDataProcessor
    {
        public async Task<string> ProcessReportDataAsync(ReportDefinition report, List<Dictionary<string, object>> data, List<ReportComponent> components)
        {
            try
            {
                var reportContent = new List<string>();
                
                // Add report header
                reportContent.Add($"Report: {report.Title}");
                reportContent.Add($"Description: {report.Description}");
                reportContent.Add($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                reportContent.Add(new string('=', 50));
                reportContent.Add("");

                if (!data.Any())
                {
                    reportContent.Add("No data found for the specified criteria.");
                    return string.Join("\n", reportContent);
                }

                // Process data based on component types
                var tableComponents = components.Where(c => c.ComponentType == "Table" || c.ComponentType == "Field").ToList();
                var chartComponents = components.Where(c => c.ComponentType == "Chart").ToList();
                var summaryComponents = components.Where(c => c.ComponentType == "Summary").ToList();

                // Process table data
                if (tableComponents.Any())
                {
                    reportContent.AddRange(ProcessTableData(data, tableComponents));
                }

                // Process summary data
                if (summaryComponents.Any())
                {
                    reportContent.AddRange(ProcessSummaryData(data, summaryComponents));
                }

                // Process chart data (for now, just add placeholder)
                if (chartComponents.Any())
                {
                    reportContent.AddRange(ProcessChartData(data, chartComponents));
                }

                return string.Join("\n", reportContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing report data: {ex.Message}", ex);
            }
        }

        private List<string> ProcessTableData(List<Dictionary<string, object>> data, List<ReportComponent> tableComponents)
        {
            var result = new List<string>();
            
            if (!data.Any())
            {
                result.Add("No data available for table display.");
                return result;
            }

            // Get column headers
            var headers = data.First().Keys.ToList();
            
            // Calculate column widths
            var columnWidths = CalculateColumnWidths(data, headers);
            
            // Add table header
            result.Add(CreateTableHeader(headers, columnWidths));
            result.Add(CreateTableSeparator(columnWidths));
            
            // Add data rows
            foreach (var row in data)
            {
                result.Add(CreateTableRow(row, headers, columnWidths));
            }
            
            result.Add(CreateTableSeparator(columnWidths));
            result.Add($"Total Rows: {data.Count}");
            result.Add("");

            return result;
        }

        private List<string> ProcessSummaryData(List<Dictionary<string, object>> data, List<ReportComponent> summaryComponents)
        {
            var result = new List<string>();
            
            if (!data.Any())
            {
                return result;
            }

            result.Add("SUMMARY STATISTICS");
            result.Add(new string('-', 20));

            foreach (var component in summaryComponents)
            {
                // For now, add basic summary statistics
                // This would need to be enhanced to parse component properties and data binding
                var totalProjects = data.Count;
                result.Add($"Total Projects: {totalProjects}");
            }

            result.Add("");
            return result;
        }

        private List<string> ProcessChartData(List<Dictionary<string, object>> data, List<ReportComponent> chartComponents)
        {
            var result = new List<string>();
            
            result.Add("CHART DATA");
            result.Add(new string('-', 12));
            result.Add("Chart components are available but require Chart.js integration for visual display.");
            result.Add($"Data points available: {data.Count}");
            result.Add("");

            return result;
        }

        private List<int> CalculateColumnWidths(List<Dictionary<string, object>> data, List<string> headers)
        {
            var widths = new List<int>();
            
            foreach (var header in headers)
            {
                var maxWidth = header.Length;
                
                foreach (var row in data.Take(100)) // Limit to first 100 rows for performance
                {
                    if (row.ContainsKey(header) && row[header] != null)
                    {
                        var value = row[header].ToString();
                        maxWidth = Math.Max(maxWidth, value.Length);
                    }
                }
                
                widths.Add(Math.Min(maxWidth + 2, 30)); // Cap at 30 characters
            }
            
            return widths;
        }

        private string CreateTableHeader(List<string> headers, List<int> columnWidths)
        {
            var headerRow = "";
            for (int i = 0; i < headers.Count; i++)
            {
                headerRow += headers[i].PadRight(columnWidths[i]);
            }
            return headerRow.TrimEnd();
        }

        private string CreateTableSeparator(List<int> columnWidths)
        {
            var separator = "";
            foreach (var width in columnWidths)
            {
                separator += new string('-', width - 1) + " ";
            }
            return separator.TrimEnd();
        }

        private string CreateTableRow(Dictionary<string, object> row, List<string> headers, List<int> columnWidths)
        {
            var rowText = "";
            for (int i = 0; i < headers.Count; i++)
            {
                var value = row.ContainsKey(headers[i]) && row[headers[i]] != null 
                    ? row[headers[i]].ToString() 
                    : "";
                rowText += value.PadRight(columnWidths[i]);
            }
            return rowText.TrimEnd();
        }

        private decimal CalculateAggregate(List<decimal> values, string aggregateFunction)
        {
            return aggregateFunction?.ToUpper() switch
            {
                "COUNT" => values.Count,
                "SUM" => values.Sum(),
                "AVG" => values.Average(),
                "MIN" => values.Min(),
                "MAX" => values.Max(),
                _ => values.Sum()
            };
        }
    }
}
