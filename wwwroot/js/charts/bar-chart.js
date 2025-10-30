class BarChart extends ChartBase {
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
      datasets.push({ label: String(series), data, backgroundColor: palette[idx % palette.length] });
      idx++;
    }

    const stacked = !!(this.properties?.bar?.stacked);
    const horizontal = !!(this.properties?.bar?.horizontal);

    return {
      type: horizontal ? 'bar' : 'bar',
      data: { labels, datasets },
      options: {
        indexAxis: horizontal ? 'y' : 'x',
        responsive: true,
        maintainAspectRatio: false,
        scales: { x: { stacked }, y: { stacked } },
        plugins: { legend: { display: true } }
      }
    };
  }
}

window.BarChart = BarChart;


