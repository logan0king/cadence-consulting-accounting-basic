/* global ChartBase */

class LineChart extends ChartBase {
  constructor(options) {
    super(options);
    this.chartType = 'line';
    if (this.dataBinding) {
      this.refresh();
    }
  }

  buildConfig(columns, rows) {
    const mapping = (this.dataBinding && this.dataBinding.mapping) || {};
    const xField = mapping.xField || columns[0];
    const yFields = (mapping.yFields && mapping.yFields.length) ? mapping.yFields : [columns[1]];
    const seriesField = mapping.seriesField || null;

    // Group data into datasets
    let labels = [];
    const seriesToData = new Map();

    rows.forEach(r => {
      const x = r[xField];
      const series = seriesField ? r[seriesField] : (yFields.length === 1 ? yFields[0] : 'Series');
      if (!labels.includes(x)) labels.push(x);
      if (!seriesToData.has(series)) seriesToData.set(series, new Map());
      const m = seriesToData.get(series);
      const value = yFields.length === 1 ? Number(r[yFields[0]] || 0) : yFields.reduce((acc, f) => acc + Number(r[f] || 0), 0);
      m.set(x, (m.get(x) || 0) + value);
    });

    const datasets = [];
    const palette = ['#4e79a7','#f28e2b','#e15759','#76b7b2','#59a14f','#edc949','#af7aa1','#ff9da7','#9c755f','#bab0ab'];
    let idx = 0;
    for (const [series, map] of seriesToData) {
      const data = labels.map(l => map.get(l) || 0);
      datasets.push({
        label: String(series),
        data,
        borderColor: palette[idx % palette.length],
        backgroundColor: palette[idx % palette.length] + '33', // 20% opacity
        borderWidth: this.properties?.line?.borderWidth || 2,
        fill: this.properties?.line?.fill !== false,
        tension: this.properties?.line?.tension || 0.4,
        pointRadius: this.properties?.line?.showPoints !== false ? (this.properties?.line?.pointRadius || 3) : 0
      });
      idx++;
    }

    return {
      type: 'line',
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            title: { display: true, text: xField }
          },
          y: {
            beginAtZero: this.properties?.line?.beginAtZero !== false,
            title: { display: true, text: yFields.join(', ') }
          }
        },
        plugins: {
          legend: { display: datasets.length > 1 },
          title: {
            display: true,
            text: this.properties.title || 'Line Chart'
          }
        }
      }
    };
  }
}

window.LineChart = LineChart;

