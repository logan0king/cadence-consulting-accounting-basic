/* global ChartBase */

class AreaChart extends ChartBase {
  constructor(options) {
    super(options);
    this.chartType = 'area';
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
      const color = palette[idx % palette.length];
      datasets.push({
        label: String(series),
        data,
        borderColor: color,
        backgroundColor: color + '66', // 40% opacity for area fill
        borderWidth: this.properties?.area?.borderWidth || 2,
        fill: true,
        tension: this.properties?.area?.tension || 0.4,
        pointRadius: this.properties?.area?.showPoints !== false ? (this.properties?.area?.pointRadius || 3) : 0
      });
      idx++;
    }

    const stacked = !!(this.properties?.area?.stacked);

    return {
      type: 'line', // Area chart is a line chart with fill: true
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            stacked,
            title: { display: true, text: xField }
          },
          y: {
            stacked,
            beginAtZero: this.properties?.area?.beginAtZero !== false,
            title: { display: true, text: yFields.join(', ') }
          }
        },
        plugins: {
          legend: { display: datasets.length > 1 },
          title: {
            display: true,
            text: this.properties.title || 'Area Chart'
          }
        }
      }
    };
  }
}

window.AreaChart = AreaChart;

