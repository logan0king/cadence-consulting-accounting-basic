/* global ChartBase */

class DoughnutChart extends ChartBase {
  constructor(options) {
    super(options);
    this.chartType = 'doughnut';
    if (this.dataBinding) {
      this.refresh();
    }
  }

  buildConfig(columns, rows) {
    const mapping = (this.dataBinding && this.dataBinding.mapping) || {};
    const labelField = mapping.labelField || mapping.xField || columns[0];
    const valueField = mapping.valueField || mapping.yFields?.[0] || columns[1];

    // Aggregate data if needed
    const aggregated = new Map();
    rows.forEach(r => {
      const label = String(r[labelField] || 'Unknown');
      const value = Number(r[valueField] || 0);
      aggregated.set(label, (aggregated.get(label) || 0) + value);
    });

    const labels = Array.from(aggregated.keys());
    const data = Array.from(aggregated.values());
    const palette = ['#4e79a7','#f28e2b','#e15759','#76b7b2','#59a14f','#edc949','#af7aa1','#ff9da7','#9c755f','#bab0ab'];
    const backgroundColors = labels.map((_, idx) => palette[idx % palette.length]);

    return {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{
          data,
          backgroundColor: backgroundColors,
          borderColor: '#fff',
          borderWidth: this.properties?.doughnut?.borderWidth || 2,
          cutout: this.properties?.doughnut?.cutout || '50%'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: this.properties?.doughnut?.showLegend !== false,
            position: this.properties?.doughnut?.legendPosition || 'right'
          },
          title: {
            display: true,
            text: this.properties.title || 'Doughnut Chart'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                const value = context.parsed;
                const percentage = ((value / total) * 100).toFixed(1);
                return `${context.label}: ${value} (${percentage}%)`;
              }
            }
          }
        }
      }
    };
  }
}

window.DoughnutChart = DoughnutChart;

