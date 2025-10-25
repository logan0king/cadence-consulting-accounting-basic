class FabricCanvasManager {
    constructor(canvasId) {
        this.canvas = new fabric.Canvas(canvasId, {
            width: 800,
            height: 600,
            backgroundColor: '#ffffff',
            selection: true,
            preserveObjectStacking: true
        });
        
        this.propertyGrid = null;
        this.setupEventHandlers();
        this.setupCanvasTools();
    }

    setupEventHandlers() {
        // Object selection events
        this.canvas.on('selection:created', (e) => {
            if (this.propertyGrid) {
                this.propertyGrid.updateProperties(e.selected[0]);
            }
        });

        this.canvas.on('selection:updated', (e) => {
            if (this.propertyGrid) {
                this.propertyGrid.updateProperties(e.selected[0]);
            }
        });

        this.canvas.on('selection:cleared', () => {
            if (this.propertyGrid) {
                this.propertyGrid.clearProperties();
            }
        });

        // Object modification events
        this.canvas.on('object:modified', (e) => {
            if (this.propertyGrid) {
                this.propertyGrid.updateProperties(e.target);
            }
        });

        // Object movement events
        this.canvas.on('object:moving', (e) => {
            this.snapToGrid(e.target);
        });

        this.canvas.on('object:scaling', (e) => {
            this.constrainScaling(e.target);
        });
    }

    setupCanvasTools() {
        // Zoom controls
        $('#zoomIn').click(() => this.zoomIn());
        $('#zoomOut').click(() => this.zoomOut());
        $('#resetZoom').click(() => this.resetZoom());

        // Alignment tools
        $('#alignLeft').click(() => this.alignObjects('left'));
        $('#alignCenter').click(() => this.alignObjects('center'));
        $('#alignRight').click(() => this.alignObjects('right'));
        $('#alignTop').click(() => this.alignObjects('top'));
        $('#alignMiddle').click(() => this.alignObjects('middle'));
        $('#alignBottom').click(() => this.alignObjects('bottom'));

        // Distribution tools
        $('#distributeHorizontally').click(() => this.distributeObjects('horizontal'));
        $('#distributeVertically').click(() => this.distributeObjects('vertical'));

        // Grouping tools
        $('#groupObjects').click(() => this.groupSelectedObjects());
        $('#ungroupObjects').click(() => this.ungroupSelectedObjects());

        // Layer tools
        $('#bringToFront').click(() => this.bringToFront());
        $('#sendToBack').click(() => this.sendToBack());
    }

    addReportComponent(type, options = {}) {
        let component;
        const defaultOptions = {
            left: options.x || 100,
            top: options.y || 100,
            width: options.width || 200,
            height: options.height || 50
        };

        switch (type) {
            case 'textbox':
                component = new fabric.Textbox(options.text || 'Text', {
                    ...defaultOptions,
                    fontSize: options.fontSize || 12,
                    fontFamily: options.fontFamily || 'Arial',
                    fill: options.fill || '#000000',
                    backgroundColor: options.backgroundColor || 'transparent',
                    textAlign: options.textAlign || 'left'
                });
                break;
                
            case 'label':
                component = new fabric.Text(options.text || 'Label', {
                    ...defaultOptions,
                    fontSize: options.fontSize || 12,
                    fontFamily: options.fontFamily || 'Arial',
                    fill: options.fill || '#000000',
                    textAlign: options.textAlign || 'left'
                });
                break;
                
            case 'table':
                component = this.createTableComponent(options);
                break;
                
            case 'list':
                component = this.createListComponent(options);
                break;
                
            case 'chart':
                component = this.createChartComponent(options);
                break;
                
            case 'subreport':
                component = this.createSubreportComponent(options);
                break;
                
            case 'panel':
                component = new fabric.Rect({
                    ...defaultOptions,
                    fill: options.fill || '#f8f9fa',
                    stroke: options.stroke || '#dee2e6',
                    strokeWidth: options.strokeWidth || 1,
                    rx: 4,
                    ry: 4
                });
                break;
                
            case 'group':
                component = this.createGroupComponent(options);
                break;
                
            case 'pagebreak':
                component = new fabric.Line([defaultOptions.left, defaultOptions.top, 
                                          defaultOptions.left + defaultOptions.width, 
                                          defaultOptions.top], {
                    stroke: '#ff0000',
                    strokeWidth: 2,
                    strokeDashArray: [10, 5]
                });
                break;
                
            case 'spacer':
                component = new fabric.Rect({
                    ...defaultOptions,
                    fill: 'transparent',
                    stroke: '#cccccc',
                    strokeWidth: 1,
                    strokeDashArray: [2, 2]
                });
                break;
                
            case 'barcode':
                component = this.createBarcodeComponent(options);
                break;
                
            case 'qrcode':
                component = this.createQRCodeComponent(options);
                break;
                
            case 'map':
                component = this.createMapComponent(options);
                break;
                
            case 'gauge':
                component = this.createGaugeComponent(options);
                break;
                
            case 'image':
                component = new fabric.Rect({
                    ...defaultOptions,
                    fill: '#f0f0f0',
                    stroke: '#cccccc',
                    strokeWidth: 1,
                    strokeDashArray: [5, 5]
                });
                // Add image placeholder text
                const imageText = new fabric.Text('Image Placeholder', {
                    left: defaultOptions.left + 10,
                    top: defaultOptions.top + 20,
                    fontSize: 12,
                    fill: '#666666'
                });
                this.canvas.add(imageText);
                break;
                
            case 'line':
                component = new fabric.Line([defaultOptions.left, defaultOptions.top, 
                                          defaultOptions.left + defaultOptions.width, 
                                          defaultOptions.top + defaultOptions.height], {
                    stroke: options.stroke || '#000000',
                    strokeWidth: options.strokeWidth || 2
                });
                break;
                
            case 'rectangle':
                component = new fabric.Rect({
                    ...defaultOptions,
                    fill: options.fill || 'transparent',
                    stroke: options.stroke || '#000000',
                    strokeWidth: options.strokeWidth || 1
                });
                break;
                
            case 'datasource':
                component = this.createDataSourceNode(options);
                break;
                
            default:
                console.warn('Unknown component type:', type);
                return;
        }

        if (component) {
            // Add all property grid properties
            this.initializeComponentProperties(component, type);
            this.canvas.add(component);
            this.canvas.setActiveObject(component);
            this.canvas.renderAll();
        }
    }

    initializeComponentProperties(component, type) {
        const defaultProperties = this.getDefaultProperties(type);
        Object.keys(defaultProperties).forEach(key => {
            component.set(key, defaultProperties[key]);
        });
    }

    getDefaultProperties(type) {
        const baseProperties = {
            // Design
            name: `${type}${Date.now()}`,
            
            // Layout
            location: `${this.canvas.getActiveObject()?.left || 0}, ${this.canvas.getActiveObject()?.top || 0}`,
            size: `${this.canvas.getActiveObject()?.width || 100}, ${this.canvas.getActiveObject()?.height || 50}`,
            snapLineMargin: '0, 0, 0, 0',
            
            // Navigation
            bookmark: '',
            navigationTarget: '',
            navigationUrl: '',
            parentBookmark: '(none)',
            
            // Printing
            rightToLeft: 'Inherit',
            
            // Appearance
            backgroundColor: 'transparent',
            borderColor: 'black',
            borderDashStyle: 'Solid',
            borderWidth: 1,
            borders: 'None',
            foregroundColor: 'black',
            textAlignment: 'Middle Left',
            textTrimming: 'Character',
            
            // Behavior
            anchorHorizontally: 'None',
            anchorVertically: 'None',
            angle: 0,
            autoWidth: false,
            canGrow: true,
            canPublish: true,
            canShrink: true,
            keepTogether: false,
            multiline: false,
            processDuplicatesMode: 'Leave',
            processDuplicatesTarget: 'Value',
            processNullValues: 'Suppress and Shrink',
            visible: true,
            wordWrap: true,
            
            // Data
            tag: '',
            text: '',
            textFormatString: '',
            xlsxFormatString: '',
            nullValueText: 'None'
        };

        return baseProperties;
    }

    snapToGrid(object) {
        const gridSize = 10;
        object.set({
            left: Math.round(object.left / gridSize) * gridSize,
            top: Math.round(object.top / gridSize) * gridSize
        });
    }

    constrainScaling(object) {
        // Prevent negative scaling
        if (object.scaleX < 0.1) object.scaleX = 0.1;
        if (object.scaleY < 0.1) object.scaleY = 0.1;
    }

    zoomIn() {
        const zoom = this.canvas.getZoom();
        this.canvas.setZoom(Math.min(zoom * 1.2, 3));
    }

    zoomOut() {
        const zoom = this.canvas.getZoom();
        this.canvas.setZoom(Math.max(zoom / 1.2, 0.1));
    }

    resetZoom() {
        this.canvas.setZoom(1);
    }

    setPropertyGrid(propertyGrid) {
        this.propertyGrid = propertyGrid;
    }

    getCanvasData() {
        return JSON.stringify(this.canvas.toJSON());
    }

    loadCanvasData(data) {
        this.canvas.loadFromJSON(data, () => {
            this.canvas.renderAll();
        });
    }

    clearCanvas() {
        this.canvas.clear();
        this.canvas.backgroundColor = '#ffffff';
        this.canvas.renderAll();
    }

    createDataSourceNode(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true,
            lockMovementX: false,
            lockMovementY: false
        });

        // Create header rectangle
        const headerRect = new fabric.Rect({
            width: 200,
            height: 30,
            fill: '#2196f3',
            stroke: '#1976d2',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        // Create header text
        const headerText = new fabric.Text(options.sourceName || 'Data Source', {
            fontSize: 14,
            fontFamily: 'Arial',
            fill: '#ffffff',
            fontWeight: 'bold',
            originX: 'center',
            originY: 'center',
            left: 100,
            top: 15
        });

        // Calculate height based on fields
        const fieldHeight = options.fields ? Math.max(options.fields.length * 20 + 10, 50) : 50;
        
        // Create fields container
        const fieldsRect = new fabric.Rect({
            width: 200,
            height: fieldHeight,
            fill: '#ffffff',
            stroke: '#e0e0e0',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top',
            top: 30
        });

        group.add(headerRect);
        group.add(headerText);
        group.add(fieldsRect);

        // Add field names
        if (options.fields && options.fields.length > 0) {
            options.fields.forEach((field, index) => {
                const fieldText = new fabric.Text(field.name, {
                    fontSize: 12,
                    fontFamily: 'Arial',
                    fill: '#333333',
                    originX: 'left',
                    originY: 'top',
                    left: 10,
                    top: 40 + (index * 20)
                });
                group.add(fieldText);
            });
        } else {
            // Add placeholder text
            const placeholderText = new fabric.Text('No fields available', {
                fontSize: 12,
                fontFamily: 'Arial',
                fill: '#999999',
                originX: 'center',
                originY: 'center',
                left: 100,
                top: 55
            });
            group.add(placeholderText);
        }

        // Set group properties
        group.set({
            sourceType: 'datasource',
            sourceName: options.sourceName,
            fields: options.fields || [],
            componentType: 'datasource'
        });

        return group;
    }

    addDataSourceFromPanel(sourceName, fields) {
        const options = {
            sourceName: sourceName,
            fields: fields,
            x: 50 + (Math.random() * 200), // Random position
            y: 50 + (Math.random() * 200)
        };
        
        this.addReportComponent('datasource', options);
    }

    createRelationshipLine(fromNode, toNode, fromField, toField) {
        const fromCenter = fromNode.getCenterPoint();
        const toCenter = toNode.getCenterPoint();
        
        const line = new fabric.Line([
            fromCenter.x, fromCenter.y,
            toCenter.x, toCenter.y
        ], {
            stroke: '#666666',
            strokeWidth: 2,
            strokeDashArray: [5, 5],
            selectable: false,
            evented: false
        });

        // Add relationship label
        const labelText = `${fromField} → ${toField}`;
        const label = new fabric.Text(labelText, {
            left: (fromCenter.x + toCenter.x) / 2,
            top: (fromCenter.y + toCenter.y) / 2 - 10,
            fontSize: 10,
            fontFamily: 'Arial',
            fill: '#666666',
            backgroundColor: '#ffffff',
            padding: 2,
            selectable: false,
            evented: false
        });

        this.canvas.add(line);
        this.canvas.add(label);
        this.canvas.renderAll();
    }

    getDataSources() {
        return this.canvas.getObjects().filter(obj => 
            obj.sourceType === 'datasource' || obj.componentType === 'datasource'
        );
    }

    getSelectedDataSource() {
        const activeObject = this.canvas.getActiveObject();
        if (activeObject && (activeObject.sourceType === 'datasource' || activeObject.componentType === 'datasource')) {
            return activeObject;
        }
        return null;
    }

    // Layout Tools
    alignObjects(alignment) {
        const activeObjects = this.canvas.getActiveObjects();
        if (activeObjects.length < 2) return;

        const bounds = this.getObjectsBounds(activeObjects);
        
        activeObjects.forEach(obj => {
            switch (alignment) {
                case 'left':
                    obj.set('left', bounds.left);
                    break;
                case 'center':
                    obj.set('left', bounds.left + bounds.width / 2 - obj.getScaledWidth() / 2);
                    break;
                case 'right':
                    obj.set('left', bounds.left + bounds.width - obj.getScaledWidth());
                    break;
                case 'top':
                    obj.set('top', bounds.top);
                    break;
                case 'middle':
                    obj.set('top', bounds.top + bounds.height / 2 - obj.getScaledHeight() / 2);
                    break;
                case 'bottom':
                    obj.set('top', bounds.top + bounds.height - obj.getScaledHeight());
                    break;
            }
        });

        this.canvas.renderAll();
    }

    distributeObjects(direction) {
        const activeObjects = this.canvas.getActiveObjects();
        if (activeObjects.length < 3) return;

        // Sort objects by position
        const sortedObjects = activeObjects.sort((a, b) => {
            return direction === 'horizontal' ? a.left - b.left : a.top - b.top;
        });

        const bounds = this.getObjectsBounds(sortedObjects);
        const totalSpace = direction === 'horizontal' ? bounds.width : bounds.height;
        const spacing = totalSpace / (sortedObjects.length - 1);

        sortedObjects.forEach((obj, index) => {
            if (index === 0 || index === sortedObjects.length - 1) return;

            if (direction === 'horizontal') {
                obj.set('left', bounds.left + (spacing * index));
            } else {
                obj.set('top', bounds.top + (spacing * index));
            }
        });

        this.canvas.renderAll();
    }

    groupSelectedObjects() {
        const activeObjects = this.canvas.getActiveObjects();
        if (activeObjects.length < 2) return;

        const group = new fabric.Group(activeObjects, {
            left: this.getObjectsBounds(activeObjects).left,
            top: this.getObjectsBounds(activeObjects).top
        });

        this.canvas.remove(...activeObjects);
        this.canvas.add(group);
        this.canvas.setActiveObject(group);
        this.canvas.renderAll();
    }

    ungroupSelectedObjects() {
        const activeObject = this.canvas.getActiveObject();
        if (!activeObject || !activeObject.type === 'group') return;

        const objects = activeObject.getObjects();
        const groupLeft = activeObject.left;
        const groupTop = activeObject.top;

        objects.forEach(obj => {
            obj.set({
                left: obj.left + groupLeft,
                top: obj.top + groupTop
            });
            this.canvas.add(obj);
        });

        this.canvas.remove(activeObject);
        this.canvas.renderAll();
    }

    bringToFront() {
        const activeObject = this.canvas.getActiveObject();
        if (!activeObject) return;

        this.canvas.bringToFront(activeObject);
        this.canvas.renderAll();
    }

    sendToBack() {
        const activeObject = this.canvas.getActiveObject();
        if (!activeObject) return;

        this.canvas.sendToBack(activeObject);
        this.canvas.renderAll();
    }

    getObjectsBounds(objects) {
        if (objects.length === 0) return { left: 0, top: 0, width: 0, height: 0 };

        let minLeft = Infinity, minTop = Infinity, maxRight = -Infinity, maxBottom = -Infinity;

        objects.forEach(obj => {
            const left = obj.left;
            const top = obj.top;
            const right = left + obj.getScaledWidth();
            const bottom = top + obj.getScaledHeight();

            minLeft = Math.min(minLeft, left);
            minTop = Math.min(minTop, top);
            maxRight = Math.max(maxRight, right);
            maxBottom = Math.max(maxBottom, bottom);
        });

        return {
            left: minLeft,
            top: minTop,
            width: maxRight - minLeft,
            height: maxBottom - minTop
        };
    }

    // Advanced Component Creators
    createTableComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        // Create table structure
        const rows = options.rows || 3;
        const cols = options.cols || 3;
        const cellWidth = options.width || 200;
        const cellHeight = options.height || 150;

        // Create table border
        const tableRect = new fabric.Rect({
            width: cellWidth,
            height: cellHeight,
            fill: 'transparent',
            stroke: '#000000',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        group.add(tableRect);

        // Create grid lines
        for (let i = 1; i < rows; i++) {
            const line = new fabric.Line([0, (cellHeight / rows) * i, cellWidth, (cellHeight / rows) * i], {
                stroke: '#000000',
                strokeWidth: 1,
                originX: 'left',
                originY: 'top'
            });
            group.add(line);
        }

        for (let i = 1; i < cols; i++) {
            const line = new fabric.Line([(cellWidth / cols) * i, 0, (cellWidth / cols) * i, cellHeight], {
                stroke: '#000000',
                strokeWidth: 1,
                originX: 'left',
                originY: 'top'
            });
            group.add(line);
        }

        group.set({
            componentType: 'table',
            rows: rows,
            cols: cols
        });

        return group;
    }

    createListComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const items = options.items || ['Item 1', 'Item 2', 'Item 3'];
        const itemHeight = 20;

        items.forEach((item, index) => {
            const bullet = new fabric.Circle({
                left: 5,
                top: (index * itemHeight) + 5,
                radius: 3,
                fill: '#000000',
                originX: 'left',
                originY: 'top'
            });

            const text = new fabric.Text(item, {
                left: 15,
                top: (index * itemHeight) + 5,
                fontSize: 12,
                fill: '#000000',
                originX: 'left',
                originY: 'top'
            });

            group.add(bullet);
            group.add(text);
        });

        group.set({
            componentType: 'list',
            items: items
        });

        return group;
    }

    createChartComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        // Create chart container
        const chartRect = new fabric.Rect({
            width: options.width || 300,
            height: options.height || 200,
            fill: '#ffffff',
            stroke: '#dee2e6',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        // Create canvas element for Chart.js
        const canvasId = `chart_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
        const chartCanvas = document.createElement('canvas');
        chartCanvas.id = canvasId;
        chartCanvas.width = (options.width || 300) - 20;
        chartCanvas.height = (options.height || 200) - 20;
        chartCanvas.style.position = 'absolute';
        chartCanvas.style.left = '10px';
        chartCanvas.style.top = '10px';

        // Create chart using Chart.js
        const chartType = options.chartType || 'bar';
        const sampleData = window.chartManager.generateSampleData(chartType);
        const chart = window.chartManager.createChart(canvasId, chartType, sampleData);

        // Create chart title
        const chartTitle = new fabric.Text(options.title || 'Chart', {
            left: (options.width || 300) / 2,
            top: 10,
            fontSize: 14,
            fill: '#333333',
            fontWeight: 'bold',
            originX: 'center',
            originY: 'top'
        });

        group.add(chartRect);
        group.add(chartTitle);

        // Store chart reference
        group.set({
            componentType: 'chart',
            chartType: chartType,
            chartId: canvasId,
            chart: chart,
            title: options.title || 'Chart'
        });

        // Add canvas to the group (this is a simplified approach)
        // In a real implementation, you'd need to handle the canvas integration differently
        const chartPlaceholder = new fabric.Rect({
            left: 10,
            top: 30,
            width: (options.width || 300) - 20,
            height: (options.height || 200) - 40,
            fill: '#f8f9fa',
            stroke: '#dee2e6',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        const chartText = new fabric.Text(`${chartType.toUpperCase()} CHART`, {
            left: (options.width || 300) / 2,
            top: (options.height || 200) / 2,
            fontSize: 12,
            fill: '#6c757d',
            originX: 'center',
            originY: 'center'
        });

        group.add(chartPlaceholder);
        group.add(chartText);

        return group;
    }

    createSubreportComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const subreportRect = new fabric.Rect({
            width: options.width || 200,
            height: options.height || 100,
            fill: '#e3f2fd',
            stroke: '#2196f3',
            strokeWidth: 2,
            originX: 'left',
            originY: 'top'
        });

        const subreportText = new fabric.Text('Subreport', {
            left: (options.width || 200) / 2,
            top: (options.height || 100) / 2,
            fontSize: 14,
            fill: '#1976d2',
            originX: 'center',
            originY: 'center'
        });

        group.add(subreportRect);
        group.add(subreportText);

        group.set({
            componentType: 'subreport',
            reportName: options.reportName || ''
        });

        return group;
    }

    createGroupComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const groupRect = new fabric.Rect({
            width: options.width || 200,
            height: options.height || 100,
            fill: 'transparent',
            stroke: '#6c757d',
            strokeWidth: 1,
            strokeDashArray: [5, 5],
            originX: 'left',
            originY: 'top'
        });

        const groupText = new fabric.Text('Group', {
            left: (options.width || 200) / 2,
            top: (options.height || 100) / 2,
            fontSize: 12,
            fill: '#6c757d',
            originX: 'center',
            originY: 'center'
        });

        group.add(groupRect);
        group.add(groupText);

        group.set({
            componentType: 'group'
        });

        return group;
    }

    createBarcodeComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const barcodeRect = new fabric.Rect({
            width: options.width || 150,
            height: options.height || 50,
            fill: '#ffffff',
            stroke: '#000000',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        const barcodeText = new fabric.Text('Barcode', {
            left: (options.width || 150) / 2,
            top: (options.height || 50) / 2,
            fontSize: 12,
            fill: '#000000',
            originX: 'center',
            originY: 'center'
        });

        group.add(barcodeRect);
        group.add(barcodeText);

        group.set({
            componentType: 'barcode',
            barcodeType: options.barcodeType || 'code128'
        });

        return group;
    }

    createQRCodeComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const qrRect = new fabric.Rect({
            width: options.width || 100,
            height: options.height || 100,
            fill: '#ffffff',
            stroke: '#000000',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        const qrText = new fabric.Text('QR Code', {
            left: (options.width || 100) / 2,
            top: (options.height || 100) / 2,
            fontSize: 10,
            fill: '#000000',
            originX: 'center',
            originY: 'center'
        });

        group.add(qrRect);
        group.add(qrText);

        group.set({
            componentType: 'qrcode'
        });

        return group;
    }

    createMapComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const mapRect = new fabric.Rect({
            width: options.width || 200,
            height: options.height || 150,
            fill: '#e8f5e8',
            stroke: '#4caf50',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        const mapText = new fabric.Text('Map', {
            left: (options.width || 200) / 2,
            top: (options.height || 150) / 2,
            fontSize: 14,
            fill: '#2e7d32',
            originX: 'center',
            originY: 'center'
        });

        group.add(mapRect);
        group.add(mapText);

        group.set({
            componentType: 'map'
        });

        return group;
    }

    createGaugeComponent(options) {
        const group = new fabric.Group([], {
            left: options.x || 100,
            top: options.y || 100,
            selectable: true,
            hasControls: true,
            hasBorders: true
        });

        const gaugeRect = new fabric.Rect({
            width: options.width || 100,
            height: options.height || 100,
            fill: '#fff3e0',
            stroke: '#ff9800',
            strokeWidth: 1,
            originX: 'left',
            originY: 'top'
        });

        const gaugeText = new fabric.Text('Gauge', {
            left: (options.width || 100) / 2,
            top: (options.height || 100) / 2,
            fontSize: 12,
            fill: '#f57c00',
            originX: 'center',
            originY: 'center'
        });

        group.add(gaugeRect);
        group.add(gaugeText);

        group.set({
            componentType: 'gauge'
        });

        return group;
    }
}
