/* global Chart */

class ChartBase {
  constructor(options) {
    this.id = options.id;
    this.position = options.position || { x: 50, y: 50 };
    this.size = options.size || { width: 400, height: 240 };
    this.properties = options.properties || {};
    this.dataBinding = options.dataBinding || null;

    this.container = document.createElement('div');
    this.container.className = 'chart-container';
    this.container.style.position = 'absolute';
    this.container.style.left = this.position.x + 'px';
    this.container.style.top = this.position.y + 'px';
    this.container.style.width = this.size.width + 'px';
    this.container.style.height = this.size.height + 'px';

    // Inline toolbar with Bind button (avoids reliance on right-click)
    const toolbar = document.createElement('div');
    toolbar.className = 'chart-toolbar';
    const bindBtn = document.createElement('button');
    bindBtn.className = 'chart-btn';
    bindBtn.textContent = 'Bind Data';
    bindBtn.addEventListener('click', (e) => {
      e.preventDefault();
      if (window.showChartBindingDialog) window.showChartBindingDialog(this);
    });
    toolbar.appendChild(bindBtn);
    this.container.appendChild(toolbar);

    const canvas = document.createElement('canvas');
    canvas.width = this.size.width;
    canvas.height = this.size.height;
    canvas.style.position = 'absolute';
    canvas.style.left = '0';
    canvas.style.top = '0';
    this.container.appendChild(canvas);

    const canvasHost = document.querySelector('#designer-canvas-host') || document.body;
    canvasHost.appendChild(this.container);

    this.ctx = canvas.getContext('2d');
    this.chart = null;

    // Handle right-click as well, but prevent default OS menu
    this.container.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      if (window.showChartBindingDialog) window.showChartBindingDialog(this);
    });

    // If no binding yet, prompt user immediately (first-run UX)
    if (!this.dataBinding) {
      setTimeout(() => {
        if (window.showChartBindingDialog) window.showChartBindingDialog(this);
      }, 0);
    }
  }

  async refresh() {
    if (!this.dataBinding) return;
    const resp = await fetch('/api/chart/preview', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dataBinding: this.dataBinding, maxRows: 1000 })
    });
    const json = await resp.json();
    if (!json.success) {
      console.error('Chart preview failed', json.error);
      return;
    }
    const config = this.buildConfig(json.columns || [], json.rows || []);
    this.render(config);
  }

  render(config) {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
    this.chart = new Chart(this.ctx, config);
  }

  // To be overridden by subclasses
  buildConfig(columns, rows) {
    return { type: 'bar', data: {}, options: {} };
  }
}

window.ChartBase = ChartBase;


