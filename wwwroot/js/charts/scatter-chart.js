/* global ChartBase */

class ScatterChart extends ChartBase {
  constructor(options) {
    super(options);
    this.chartType = 'scatter';
    if (this.dataBinding) {
      this.refresh();
    }
  }

  buildConfig(columns, rows) {
    const mapping = (this.dataBinding && this.dataBinding.mapping) || {};
    const xField = mapping.xField || columns[0];
    const yFields = (mapping.yFields && mapping.yFields.length) ? mapping.yFields : [columns[1]];
    const seriesField = mapping.seriesField || null;

    // Group data by series
    const seriesToPoints = new Map();

    rows.forEach(r => {
      const x = Number(r[xField] || 0);
      const series = seriesField ? r[seriesField] : (yFields.length === 1 ? yFields[0] : 'Series');
      if (!seriesToPoints.has(series)) seriesToPoints.set(series, []);
      
      yFields.forEach(yField => {
        const y = Number(r[yField] || 0);
        seriesToPoints.get(series).push({ x, y });
      });
    });

    const datasets = [];
    const palette = ['#4e79a7','#f28e2b','#e15759','#76b7b2','#59a14f','#edc949','#af7aa1','#ff9da7','#9c755f','#bab0ab'];
    let idx = 0;
    for (const [series, points] of seriesToPoints) {
      datasets.push({
        label: String(series),
        data: points,
        backgroundColor: palette[idx % palette.length] + '80', // 50% opacity
        borderColor: palette[idx % palette.length],
        pointRadius: this.properties?.scatter?.pointRadius || 5,
        pointHoverRadius: this.properties?.scatter?.pointHoverRadius || 7
      });
      idx++;
    }

    return {
      type: 'scatter',
      data: { datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            type: 'linear',
            position: 'bottom',
            title: { display: true, text: xField }
          },
          y: {
            title: { display: true, text: yFields.join(', ') }
          }
        },
        plugins: {
          legend: { display: datasets.length > 1 },
          title: {
            display: true,
            text: this.properties.title || 'Scatter Plot'
          }
        }
      }
    };
  }
}

window.ScatterChart = ScatterChart;

