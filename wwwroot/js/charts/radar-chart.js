/* global ChartBase */

class RadarChart extends ChartBase {
  constructor(options) {
    super(options);
    this.chartType = 'radar';
    if (this.dataBinding) {
      this.refresh();
    }
  }

  buildConfig(columns, rows) {
    const mapping = (this.dataBinding && this.dataBinding.mapping) || {};
    const labelField = mapping.labelField || mapping.xField || columns[0];
    const valueField = mapping.valueField || mapping.yFields?.[0] || columns[1];
    const seriesField = mapping.seriesField || null;

    // Get unique labels (categories)
    const labels = [...new Set(rows.map(r => String(r[labelField] || 'Unknown')))];
    
    // Group by series if provided
    const seriesToData = new Map();
    rows.forEach(r => {
      const label = String(r[labelField] || 'Unknown');
      const value = Number(r[valueField] || 0);
      const series = seriesField ? String(r[seriesField] || 'Default') : 'Series';
      
      if (!seriesToData.has(series)) {
        seriesToData.set(series, new Map());
      }
      const seriesMap = seriesToData.get(series);
      seriesMap.set(label, (seriesMap.get(label) || 0) + value);
    });

    const datasets = [];
    const palette = ['#4e79a7','#f28e2b','#e15759','#76b7b2','#59a14f','#edc949','#af7aa1','#ff9da7','#9c755f','#bab0ab'];
    let idx = 0;
    for (const [series, map] of seriesToData) {
      const data = labels.map(l => map.get(l) || 0);
      const color = palette[idx % palette.length];
      datasets.push({
        label: series,
        data,
        borderColor: color,
        backgroundColor: color + '33', // 20% opacity
        borderWidth: this.properties?.radar?.borderWidth || 2,
        pointBackgroundColor: color,
        pointBorderColor: '#fff',
        pointHoverBackgroundColor: '#fff',
        pointHoverBorderColor: color
      });
      idx++;
    }

    return {
      type: 'radar',
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          r: {
            beginAtZero: this.properties?.radar?.beginAtZero !== false,
            ticks: {
              display: this.properties?.radar?.showTicks !== false
            }
          }
        },
        plugins: {
          legend: { display: datasets.length > 1 },
          title: {
            display: true,
            text: this.properties.title || 'Radar Chart'
          }
        }
      }
    };
  }
}

window.RadarChart = RadarChart;

