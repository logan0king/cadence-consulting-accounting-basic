class ExportManager {
    constructor() {
        this.supportedFormats = ['pdf', 'excel', 'csv', 'html'];
    }

    async exportReport(reportData, format, filename = 'report') {
        switch (format.toLowerCase()) {
            case 'pdf':
                return await this.exportToPdf(reportData, filename);
            case 'excel':
                return await this.exportToExcel(reportData, filename);
            case 'csv':
                return await this.exportToCsv(reportData, filename);
            case 'html':
                return await this.exportToHtml(reportData, filename);
            default:
                throw new Error(`Unsupported export format: ${format}`);
        }
    }

    async exportToPdf(reportData, filename) {
        // This would typically call the server-side PDF generation
        // For now, we'll show a message
        alert('PDF export will be handled by the server-side PDF generator');
        return null;
    }

    async exportToExcel(reportData, filename) {
        try {
            const workbook = XLSX.utils.book_new();
            
            // Create worksheet from report data
            const worksheet = XLSX.utils.json_to_sheet(reportData.rows);
            
            // Add worksheet to workbook
            XLSX.utils.book_append_sheet(workbook, worksheet, 'Report Data');
            
            // Generate Excel file
            const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
            
            // Create blob and download
            const blob = new Blob([excelBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
            this.downloadFile(blob, `${filename}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
            
            return true;
        } catch (error) {
            console.error('Excel export error:', error);
            throw new Error('Failed to export to Excel: ' + error.message);
        }
    }

    async exportToCsv(reportData, filename) {
        try {
            if (!reportData.rows || reportData.rows.length === 0) {
                throw new Error('No data to export');
            }

            // Get column headers
            const headers = reportData.columns.map(col => col.name);
            
            // Create CSV content
            let csvContent = headers.join(',') + '\n';
            
            // Add data rows
            reportData.rows.forEach(row => {
                const values = headers.map(header => {
                    const value = row[header];
                    // Escape values that contain commas or quotes
                    if (typeof value === 'string' && (value.includes(',') || value.includes('"') || value.includes('\n'))) {
                        return `"${value.replace(/"/g, '""')}"`;
                    }
                    return value || '';
                });
                csvContent += values.join(',') + '\n';
            });

            // Create blob and download
            const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
            this.downloadFile(blob, `${filename}.csv`, 'text/csv');
            
            return true;
        } catch (error) {
            console.error('CSV export error:', error);
            throw new Error('Failed to export to CSV: ' + error.message);
        }
    }

    async exportToHtml(reportData, filename) {
        try {
            let htmlContent = `
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>${filename}</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        table { border-collapse: collapse; width: 100%; margin-top: 20px; }
                        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                        th { background-color: #f2f2f2; font-weight: bold; }
                        tr:nth-child(even) { background-color: #f9f9f9; }
                        .header { margin-bottom: 20px; }
                        .summary { margin-top: 20px; font-size: 14px; color: #666; }
                    </style>
                </head>
                <body>
                    <div class="header">
                        <h1>Report Export</h1>
                        <p>Generated on: ${new Date().toLocaleString()}</p>
                    </div>
            `;

            if (reportData.rows && reportData.rows.length > 0) {
                htmlContent += '<table>';
                
                // Add headers
                htmlContent += '<thead><tr>';
                reportData.columns.forEach(col => {
                    htmlContent += `<th>${this.escapeHtml(col.name)}</th>`;
                });
                htmlContent += '</tr></thead>';
                
                // Add data rows
                htmlContent += '<tbody>';
                reportData.rows.forEach(row => {
                    htmlContent += '<tr>';
                    reportData.columns.forEach(col => {
                        const value = row[col.name] || '';
                        htmlContent += `<td>${this.escapeHtml(value.toString())}</td>`;
                    });
                    htmlContent += '</tr>';
                });
                htmlContent += '</tbody></table>';
                
                // Add summary
                htmlContent += `<div class="summary">Total rows: ${reportData.rows.length}</div>`;
            } else {
                htmlContent += '<p>No data available for export.</p>';
            }

            htmlContent += '</body></html>';

            // Create blob and download
            const blob = new Blob([htmlContent], { type: 'text/html;charset=utf-8;' });
            this.downloadFile(blob, `${filename}.html`, 'text/html');
            
            return true;
        } catch (error) {
            console.error('HTML export error:', error);
            throw new Error('Failed to export to HTML: ' + error.message);
        }
    }

    downloadFile(blob, filename, mimeType) {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.display = 'none';
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up the URL object
        window.URL.revokeObjectURL(url);
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Get available export formats
    getSupportedFormats() {
        return this.supportedFormats;
    }

    // Format file size for display
    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Validate data before export
    validateData(reportData) {
        if (!reportData) {
            throw new Error('No report data provided');
        }
        
        if (!reportData.rows || reportData.rows.length === 0) {
            throw new Error('No data rows to export');
        }
        
        if (!reportData.columns || reportData.columns.length === 0) {
            throw new Error('No column information available');
        }
        
        return true;
    }
}

// Global instance
window.exportManager = new ExportManager();
