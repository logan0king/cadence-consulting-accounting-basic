class ChartManager {
    constructor() {
        this.charts = new Map();
        this.chartTypes = [
            { value: 'bar', label: 'Bar Chart', icon: 'fas fa-chart-bar' },
            { value: 'line', label: 'Line Chart', icon: 'fas fa-chart-line' },
            { value: 'pie', label: 'Pie Chart', icon: 'fas fa-chart-pie' },
            { value: 'doughnut', label: 'Doughnut Chart', icon: 'fas fa-chart-pie' },
            { value: 'area', label: 'Area Chart', icon: 'fas fa-chart-area' },
            { value: 'scatter', label: 'Scatter Plot', icon: 'fas fa-chart-scatter' },
            { value: 'radar', label: 'Radar Chart', icon: 'fas fa-chart-radar' },
            { value: 'polarArea', label: 'Polar Area', icon: 'fas fa-chart-pie' }
        ];
    }

    createChart(canvasId, chartType, data, options = {}) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas element not found:', canvasId);
            return null;
        }

        // Destroy existing chart if it exists
        if (this.charts.has(canvasId)) {
            this.charts.get(canvasId).destroy();
        }

        const defaultOptions = this.getDefaultOptions(chartType);
        const mergedOptions = { ...defaultOptions, ...options };

        const chart = new Chart(canvas, {
            type: chartType,
            data: data,
            options: mergedOptions
        });

        this.charts.set(canvasId, chart);
        return chart;
    }

    getDefaultOptions(chartType) {
        const baseOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                title: {
                    display: true,
                    text: 'Chart Title'
                }
            }
        };

        switch (chartType) {
            case 'bar':
                return {
                    ...baseOptions,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                };
            case 'line':
                return {
                    ...baseOptions,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    elements: {
                        line: {
                            tension: 0.1
                        }
                    }
                };
            case 'pie':
            case 'doughnut':
                return {
                    ...baseOptions,
                    plugins: {
                        ...baseOptions.plugins,
                        legend: {
                            position: 'right'
                        }
                    }
                };
            case 'area':
                return {
                    ...baseOptions,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    elements: {
                        line: {
                            tension: 0.1
                        }
                    },
                    fill: true
                };
            case 'scatter':
                return {
                    ...baseOptions,
                    scales: {
                        x: {
                            type: 'linear',
                            position: 'bottom'
                        }
                    }
                };
            case 'radar':
                return {
                    ...baseOptions,
                    scales: {
                        r: {
                            beginAtZero: true
                        }
                    }
                };
            default:
                return baseOptions;
        }
    }

    generateSampleData(chartType, dataSource = null) {
        switch (chartType) {
            case 'bar':
            case 'line':
            case 'area':
                return {
                    labels: ['January', 'February', 'March', 'April', 'May', 'June'],
                    datasets: [{
                        label: 'Sample Data',
                        data: [12, 19, 3, 5, 2, 3],
                        backgroundColor: this.getRandomColors(6),
                        borderColor: this.getRandomColors(6),
                        borderWidth: 2
                    }]
                };
            case 'pie':
            case 'doughnut':
            case 'polarArea':
                return {
                    labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
                    datasets: [{
                        data: [12, 19, 3, 5, 2, 3],
                        backgroundColor: this.getRandomColors(6),
                        borderColor: this.getRandomColors(6),
                        borderWidth: 2
                    }]
                };
            case 'scatter':
                return {
                    datasets: [{
                        label: 'Sample Data',
                        data: [
                            { x: 10, y: 20 },
                            { x: 15, y: 10 },
                            { x: 20, y: 30 },
                            { x: 25, y: 15 },
                            { x: 30, y: 25 }
                        ],
                        backgroundColor: 'rgba(54, 162, 235, 0.6)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 2
                    }]
                };
            case 'radar':
                return {
                    labels: ['Eating', 'Drinking', 'Sleeping', 'Designing', 'Coding', 'Cycling'],
                    datasets: [{
                        label: 'Sample Data',
                        data: [65, 59, 90, 81, 56, 55],
                        backgroundColor: 'rgba(54, 162, 235, 0.2)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 2
                    }]
                };
            default:
                return { labels: [], datasets: [] };
        }
    }

    getRandomColors(count) {
        const colors = [];
        for (let i = 0; i < count; i++) {
            colors.push(`rgba(${Math.floor(Math.random() * 256)}, ${Math.floor(Math.random() * 256)}, ${Math.floor(Math.random() * 256)}, 0.8)`);
        }
        return colors;
    }

    updateChart(canvasId, data) {
        const chart = this.charts.get(canvasId);
        if (chart) {
            chart.data = data;
            chart.update();
        }
    }

    destroyChart(canvasId) {
        const chart = this.charts.get(canvasId);
        if (chart) {
            chart.destroy();
            this.charts.delete(canvasId);
        }
    }

    getChartTypes() {
        return this.chartTypes;
    }

    exportChartAsImage(canvasId, format = 'png') {
        const chart = this.charts.get(canvasId);
        if (chart) {
            return chart.toBase64Image(format);
        }
        return null;
    }

    // Data binding methods for report integration
    bindToDataSource(chartId, dataSource, fieldMappings) {
        // This would integrate with the report data system
        // For now, return sample data
        return this.generateSampleData('bar');
    }

    // Chart configuration for property grid
    getChartProperties(chartType) {
        const baseProperties = {
            title: { type: 'text', label: 'Chart Title', value: 'Chart Title' },
            showLegend: { type: 'checkbox', label: 'Show Legend', value: true },
            legendPosition: { type: 'select', label: 'Legend Position', value: 'top', options: ['top', 'bottom', 'left', 'right'] },
            backgroundColor: { type: 'color', label: 'Background Color', value: '#ffffff' },
            borderColor: { type: 'color', label: 'Border Color', value: '#000000' },
            borderWidth: { type: 'number', label: 'Border Width', value: 1 }
        };

        const typeSpecificProperties = {
            bar: {
                ...baseProperties,
                barThickness: { type: 'number', label: 'Bar Thickness', value: 'auto' },
                categoryPercentage: { type: 'number', label: 'Category Percentage', value: 0.8 },
                barPercentage: { type: 'number', label: 'Bar Percentage', value: 0.9 }
            },
            line: {
                ...baseProperties,
                tension: { type: 'number', label: 'Line Tension', value: 0.1 },
                fill: { type: 'checkbox', label: 'Fill Area', value: false },
                pointRadius: { type: 'number', label: 'Point Radius', value: 3 }
            },
            pie: {
                ...baseProperties,
                cutout: { type: 'number', label: 'Cutout Percentage', value: 0 },
                rotation: { type: 'number', label: 'Rotation (degrees)', value: 0 }
            },
            doughnut: {
                ...baseProperties,
                cutout: { type: 'number', label: 'Cutout Percentage', value: 50 },
                rotation: { type: 'number', label: 'Rotation (degrees)', value: 0 }
            }
        };

        return typeSpecificProperties[chartType] || baseProperties;
    }
}

// Global instance
window.chartManager = new ChartManager();
