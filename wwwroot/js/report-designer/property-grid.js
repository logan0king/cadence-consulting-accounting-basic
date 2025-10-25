class PropertyGrid {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.selectedObject = null;
        this.viewMode = 'categorized'; // 'categorized' or 'alphabetical'
        
        this.categories = {
            'Design': ['name', 'tag', 'description', 'componentType'],
            'Layout': ['location', 'size', 'snapLineMargin', 'zIndex', 'anchorHorizontally', 'anchorVertically', 'position', 'dimensions'],
            'Navigation': ['bookmark', 'navigationTarget', 'navigationUrl', 'parentBookmark', 'action', 'actionParameter', 'drillThrough', 'drillThroughReport'],
            'Printing': ['rightToLeft', 'printOnFirstPage', 'printOnLastPage', 'printIfEmpty', 'pageBreakAtStart', 'pageBreakAtEnd', 'printLocation', 'printCondition'],
            'Appearance': ['backgroundColor', 'borderColor', 'borderDashStyle', 'borderWidth', 'borders', 'foregroundColor', 'textAlignment', 'textTrimming', 'font', 'padding', 'stylePriority', 'styles', 'formattingRules', 'opacity', 'shadow', 'gradient'],
            'Behavior': ['angle', 'autoWidth', 'canGrow', 'canPublish', 'canShrink', 'keepTogether', 'multiline', 'processDuplicatesMode', 'processDuplicatesTarget', 'processNullValues', 'visible', 'wordWrap', 'interactiveSorting', 'editOptions', 'scripts', 'tooltip', 'contextMenu'],
            'Data': ['dataBindings', 'lines', 'nullValueText', 'summary', 'text', 'textFormatString', 'xlsxFormatString', 'value', 'expression', 'aggregate', 'groupBy', 'sortBy', 'filter', 'dataSource', 'fieldName', 'dataType']
        };
        
        this.setupPropertyGrid();
        this.setupEventHandlers();
    }

    setupPropertyGrid() {
        this.container.innerHTML = `
            <div class="property-grid">
                <div class="property-content" id="propertyContent">
                    <div class="no-selection text-center p-3">
                        <i class="fas fa-mouse-pointer text-muted fa-2x mb-2"></i>
                        <div class="text-muted">Select an object to edit properties</div>
                    </div>
                </div>
            </div>
        `;
    }

    setupEventHandlers() {
        // View mode toggle
        $('#categorizedView').click(() => {
            this.setViewMode('categorized');
        });
        
        $('#alphabeticalView').click(() => {
            this.setViewMode('alphabetical');
        });
    }

    setViewMode(mode) {
        this.viewMode = mode;
        
        // Update button states
        $('#categorizedView, #alphabeticalView').removeClass('active');
        $(`#${mode}View`).addClass('active');
        
        // Re-render properties
        if (this.selectedObject) {
            this.renderProperties();
        }
    }

    updateProperties(object) {
        this.selectedObject = object;
        this.renderProperties();
    }

    clearProperties() {
        this.selectedObject = null;
        this.renderProperties();
    }

    renderProperties() {
        const content = document.getElementById('propertyContent');
        if (!this.selectedObject) {
            content.innerHTML = `
                <div class="no-selection text-center p-3">
                    <i class="fas fa-mouse-pointer text-muted fa-2x mb-2"></i>
                    <div class="text-muted">Select an object to edit properties</div>
                </div>
            `;
            return;
        }

        let html = '';
        
        if (this.viewMode === 'categorized') {
            html = this.renderCategorizedView();
        } else {
            html = this.renderAlphabeticalView();
        }
        
        content.innerHTML = html;
        this.setupPropertyEventHandlers();
    }

    renderCategorizedView() {
        let html = '';
        Object.keys(this.categories).forEach(category => {
            html += this.renderCategory(category, this.categories[category]);
        });
        return html;
    }

    renderAlphabeticalView() {
        // Flatten all properties and sort alphabetically
        const allProperties = [];
        Object.keys(this.categories).forEach(category => {
            this.categories[category].forEach(property => {
                allProperties.push({ property, category });
            });
        });
        
        allProperties.sort((a, b) => a.property.localeCompare(b.property));
        
        let html = '';
        allProperties.forEach(({ property, category }) => {
            html += this.renderProperty(property, category);
        });
        
        return html;
    }

    renderCategory(categoryName, properties) {
        let categoryHtml = `
            <div class="property-category">
                <div class="category-header" onclick="toggleCategory('${categoryName}')">
                    <span class="category-name">${categoryName}</span>
                    <span class="category-toggle">+</span>
                </div>
                <div class="category-content" id="${categoryName}Content" style="display: none;">
        `;

        properties.forEach(property => {
            const value = this.selectedObject[property] || this.getDefaultValue(property);
            categoryHtml += this.renderProperty(property, categoryName, value);
        });

        categoryHtml += `
                </div>
            </div>
        `;

        return categoryHtml;
    }

    renderProperty(propertyName, category, value = null) {
        if (value === null) {
            value = this.selectedObject[propertyName] || this.getDefaultValue(propertyName);
        }
        
        const inputType = this.getInputType(propertyName);
        const displayName = this.formatPropertyName(propertyName);

        return `
            <div class="property-row">
                <label class="property-name">${displayName}</label>
                <div class="property-value">
                    ${this.renderPropertyInput(propertyName, value, inputType)}
                </div>
            </div>
        `;
    }

    getInputType(propertyName) {
        const inputTypes = {
            // Color inputs
            'backgroundColor': 'color',
            'borderColor': 'color',
            'foregroundColor': 'color',
            'shadow': 'color',
            
            // Select inputs
            'borderDashStyle': 'select',
            'textAlignment': 'select',
            'textTrimming': 'select',
            'anchorHorizontally': 'select',
            'anchorVertically': 'select',
            'processDuplicatesMode': 'select',
            'processDuplicatesTarget': 'select',
            'processNullValues': 'select',
            'rightToLeft': 'select',
            'componentType': 'select',
            'dataType': 'select',
            'aggregate': 'select',
            'printLocation': 'select',
            'action': 'select',
            'borders': 'select',
            'gradient': 'select',
            
            // Checkbox inputs
            'autoWidth': 'checkbox',
            'canGrow': 'checkbox',
            'canPublish': 'checkbox',
            'canShrink': 'checkbox',
            'keepTogether': 'checkbox',
            'multiline': 'checkbox',
            'visible': 'checkbox',
            'wordWrap': 'checkbox',
            'printOnFirstPage': 'checkbox',
            'printOnLastPage': 'checkbox',
            'printIfEmpty': 'checkbox',
            'pageBreakAtStart': 'checkbox',
            'pageBreakAtEnd': 'checkbox',
            'drillThrough': 'checkbox',
            'interactiveSorting': 'checkbox',
            
            // Number inputs
            'borderWidth': 'number',
            'angle': 'number',
            'opacity': 'number',
            'zIndex': 'number',
            
            // Coordinate inputs
            'location': 'coordinates',
            'size': 'coordinates',
            'snapLineMargin': 'coordinates',
            'position': 'coordinates',
            'dimensions': 'coordinates',
            'padding': 'coordinates',
            
            // Textarea inputs
            'description': 'textarea',
            'expression': 'textarea',
            'filter': 'textarea',
            'printCondition': 'textarea',
            'tooltip': 'textarea',
            'actionParameter': 'textarea',
            
            // URL inputs
            'navigationUrl': 'url',
            'drillThroughReport': 'url',
            
            // Collapsible sections
            'font': 'collapsible',
            'stylePriority': 'collapsible',
            'styles': 'collapsible',
            'formattingRules': 'collapsible',
            'editOptions': 'collapsible',
            'scripts': 'collapsible',
            'dataBindings': 'collapsible',
            'summary': 'collapsible',
            'contextMenu': 'collapsible',
            'groupBy': 'collapsible',
            'sortBy': 'collapsible',
            'lines': 'collapsible'
        };

        return inputTypes[propertyName] || 'text';
    }

    renderPropertyInput(propertyName, value, inputType) {
        switch (inputType) {
            case 'color':
                return `<input type="color" class="form-control form-control-sm" value="${value}" onchange="updateProperty('${propertyName}', this.value)">`;
            case 'select':
                return `<select class="form-select form-select-sm" onchange="updateProperty('${propertyName}', this.value)">${this.getSelectOptions(propertyName, value)}</select>`;
            case 'checkbox':
                return `<input type="checkbox" class="form-check-input" ${value ? 'checked' : ''} onchange="updateProperty('${propertyName}', this.checked)">`;
            case 'number':
                return `<input type="number" class="form-control form-control-sm" value="${value}" onchange="updateProperty('${propertyName}', this.value)">`;
            case 'coordinates':
                return this.renderCoordinatesInput(propertyName, value);
            case 'textarea':
                return `<textarea class="form-control form-control-sm" rows="2" onchange="updateProperty('${propertyName}', this.value)">${value}</textarea>`;
            case 'url':
                return `<input type="url" class="form-control form-control-sm" value="${value}" onchange="updateProperty('${propertyName}', this.value)">`;
            case 'collapsible':
                return this.renderCollapsibleSection(propertyName, value);
            default:
                return `<input type="text" class="form-control form-control-sm" value="${value}" onchange="updateProperty('${propertyName}', this.value)">`;
        }
    }

    renderCoordinatesInput(propertyName, value) {
        const coords = value ? value.split(', ') : ['0', '0'];
        return `
            <div class="coordinates-input d-flex align-items-center gap-1">
                <input type="number" class="form-control form-control-sm" value="${coords[0]}" placeholder="X" 
                       onchange="updateCoordinate('${propertyName}', 0, this.value)">
                <span class="text-muted">,</span>
                <input type="number" class="form-control form-control-sm" value="${coords[1]}" placeholder="Y" 
                       onchange="updateCoordinate('${propertyName}', 1, this.value)">
            </div>
        `;
    }

    getSelectOptions(propertyName, currentValue) {
        const options = {
            'borderDashStyle': ['Solid', 'Dashed', 'Dotted', 'DashDot', 'DashDotDot'],
            'textAlignment': ['Left', 'Center', 'Right', 'Justify', 'Middle Left', 'Middle Center', 'Middle Right'],
            'textTrimming': ['None', 'Character', 'Word'],
            'anchorHorizontally': ['None', 'Left', 'Right', 'Both'],
            'anchorVertically': ['None', 'Top', 'Bottom', 'Both'],
            'processDuplicatesMode': ['Leave', 'Merge', 'Suppress'],
            'processDuplicatesTarget': ['Value', 'Label'],
            'processNullValues': ['Leave', 'Suppress', 'Suppress and Shrink'],
            'rightToLeft': ['Inherit', 'Yes', 'No'],
            'componentType': ['textbox', 'table', 'image', 'line', 'rectangle', 'datasource', 'field'],
            'dataType': ['string', 'int', 'decimal', 'datetime', 'boolean', 'guid', 'text'],
            'aggregate': ['None', 'Sum', 'Count', 'Average', 'Min', 'Max', 'First', 'Last'],
            'printLocation': ['Body', 'Header', 'Footer', 'PageHeader', 'PageFooter'],
            'action': ['None', 'Navigate', 'DrillThrough', 'Bookmark', 'URL'],
            'borders': ['None', 'All', 'Left', 'Right', 'Top', 'Bottom'],
            'gradient': ['None', 'Linear', 'Radial', 'Diagonal']
        };

        const optionList = options[propertyName] || [];
        return optionList.map(option => 
            `<option value="${option}" ${option === currentValue ? 'selected' : ''}>${option}</option>`
        ).join('');
    }

    getDefaultValue(propertyName) {
        const defaults = {
            // Design
            'name': 'component1',
            'tag': '',
            'description': '',
            'componentType': 'textbox',
            
            // Layout
            'location': '0, 0',
            'size': '100, 50',
            'snapLineMargin': '0, 0, 0, 0',
            'zIndex': 0,
            'anchorHorizontally': 'None',
            'anchorVertically': 'None',
            'position': '0, 0',
            'dimensions': '100, 50',
            
            // Navigation
            'bookmark': '',
            'navigationTarget': '',
            'navigationUrl': '',
            'parentBookmark': '(none)',
            'action': 'None',
            'actionParameter': '',
            'drillThrough': false,
            'drillThroughReport': '',
            
            // Printing
            'rightToLeft': 'Inherit',
            'printOnFirstPage': true,
            'printOnLastPage': true,
            'printIfEmpty': false,
            'pageBreakAtStart': false,
            'pageBreakAtEnd': false,
            'printLocation': 'Body',
            'printCondition': '',
            
            // Appearance
            'backgroundColor': 'transparent',
            'borderColor': 'black',
            'borderDashStyle': 'Solid',
            'borderWidth': 1,
            'borders': 'None',
            'foregroundColor': 'black',
            'textAlignment': 'Middle Left',
            'textTrimming': 'Character',
            'font': 'Arial, 12pt',
            'padding': '0, 0, 0, 0',
            'stylePriority': 'Normal',
            'styles': '',
            'formattingRules': '',
            'opacity': 1,
            'shadow': 'transparent',
            'gradient': 'None',
            
            // Behavior
            'angle': 0,
            'autoWidth': false,
            'canGrow': true,
            'canPublish': true,
            'canShrink': true,
            'keepTogether': false,
            'multiline': false,
            'processDuplicatesMode': 'Leave',
            'processDuplicatesTarget': 'Value',
            'processNullValues': 'Suppress and Shrink',
            'visible': true,
            'wordWrap': true,
            'interactiveSorting': false,
            'editOptions': '',
            'scripts': '',
            'tooltip': '',
            'contextMenu': '',
            
            // Data
            'dataBindings': '',
            'lines': '',
            'nullValueText': 'None',
            'summary': '',
            'text': '',
            'textFormatString': '',
            'xlsxFormatString': '',
            'value': '',
            'expression': '',
            'aggregate': 'None',
            'groupBy': '',
            'sortBy': '',
            'filter': '',
            'dataSource': '',
            'fieldName': '',
            'dataType': 'string'
        };

        return defaults[propertyName] || '';
    }

    formatPropertyName(propertyName) {
        return propertyName
            .replace(/([A-Z])/g, ' $1')
            .replace(/^./, str => str.toUpperCase())
            .trim();
    }

    setupPropertyEventHandlers() {
        // This will be called after rendering to set up any additional event handlers
    }

    renderCollapsibleSection(propertyName, value) {
        const sectionId = `${propertyName}Section`;
        return `
            <div class="collapsible-section">
                <button class="btn btn-sm btn-outline-secondary w-100 text-start" 
                        type="button" 
                        data-bs-toggle="collapse" 
                        data-bs-target="#${sectionId}" 
                        aria-expanded="false">
                    <i class="fas fa-chevron-right me-2"></i>
                    ${this.formatPropertyName(propertyName)}
                </button>
                <div class="collapse" id="${sectionId}">
                    <div class="p-2 border-top">
                        ${this.renderCollapsibleContent(propertyName, value)}
                    </div>
                </div>
            </div>
        `;
    }

    renderCollapsibleContent(propertyName, value) {
        switch (propertyName) {
            case 'font':
                return this.renderFontProperties(value);
            case 'padding':
                return this.renderPaddingProperties(value);
            case 'dataBindings':
                return this.renderDataBindingProperties(value);
            case 'styles':
                return this.renderStyleProperties(value);
            case 'formattingRules':
                return this.renderFormattingRules(value);
            case 'scripts':
                return this.renderScriptProperties(value);
            case 'summary':
                return this.renderSummaryProperties(value);
            case 'groupBy':
                return this.renderGroupByProperties(value);
            case 'sortBy':
                return this.renderSortByProperties(value);
            case 'lines':
                return this.renderLineProperties(value);
            default:
                return `<textarea class="form-control form-control-sm" rows="3">${value}</textarea>`;
        }
    }

    renderFontProperties(value) {
        const font = value ? JSON.parse(value) : { family: 'Arial', size: 12, weight: 'normal', style: 'normal' };
        return `
            <div class="row g-2">
                <div class="col-6">
                    <label class="form-label small">Family</label>
                    <input type="text" class="form-control form-control-sm" value="${font.family}" 
                           onchange="updateFontProperty('family', this.value)">
                </div>
                <div class="col-6">
                    <label class="form-label small">Size</label>
                    <input type="number" class="form-control form-control-sm" value="${font.size}" 
                           onchange="updateFontProperty('size', this.value)">
                </div>
                <div class="col-6">
                    <label class="form-label small">Weight</label>
                    <select class="form-select form-select-sm" onchange="updateFontProperty('weight', this.value)">
                        <option value="normal" ${font.weight === 'normal' ? 'selected' : ''}>Normal</option>
                        <option value="bold" ${font.weight === 'bold' ? 'selected' : ''}>Bold</option>
                        <option value="lighter" ${font.weight === 'lighter' ? 'selected' : ''}>Lighter</option>
                    </select>
                </div>
                <div class="col-6">
                    <label class="form-label small">Style</label>
                    <select class="form-select form-select-sm" onchange="updateFontProperty('style', this.value)">
                        <option value="normal" ${font.style === 'normal' ? 'selected' : ''}>Normal</option>
                        <option value="italic" ${font.style === 'italic' ? 'selected' : ''}>Italic</option>
                        <option value="oblique" ${font.style === 'oblique' ? 'selected' : ''}>Oblique</option>
                    </select>
                </div>
            </div>
        `;
    }

    renderPaddingProperties(value) {
        const padding = value ? value.split(', ') : ['0', '0', '0', '0'];
        return `
            <div class="row g-2">
                <div class="col-3">
                    <label class="form-label small">Top</label>
                    <input type="number" class="form-control form-control-sm" value="${padding[0]}" 
                           onchange="updatePaddingProperty(0, this.value)">
                </div>
                <div class="col-3">
                    <label class="form-label small">Right</label>
                    <input type="number" class="form-control form-control-sm" value="${padding[1]}" 
                           onchange="updatePaddingProperty(1, this.value)">
                </div>
                <div class="col-3">
                    <label class="form-label small">Bottom</label>
                    <input type="number" class="form-control form-control-sm" value="${padding[2]}" 
                           onchange="updatePaddingProperty(2, this.value)">
                </div>
                <div class="col-3">
                    <label class="form-label small">Left</label>
                    <input type="number" class="form-control form-control-sm" value="${padding[3]}" 
                           onchange="updatePaddingProperty(3, this.value)">
                </div>
            </div>
        `;
    }

    renderDataBindingProperties(value) {
        const bindings = value ? JSON.parse(value) : [];
        return `
            <div class="data-bindings">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <span class="small">Data Bindings</span>
                    <button class="btn btn-sm btn-outline-primary" onclick="addDataBinding()">
                        <i class="fas fa-plus"></i>
                    </button>
                </div>
                <div class="bindings-list">
                    ${bindings.map((binding, index) => `
                        <div class="binding-item border rounded p-2 mb-2">
                            <div class="row g-2">
                                <div class="col-6">
                                    <input type="text" class="form-control form-control-sm" 
                                           placeholder="Property" value="${binding.property}">
                                </div>
                                <div class="col-6">
                                    <input type="text" class="form-control form-control-sm" 
                                           placeholder="Expression" value="${binding.expression}">
                                </div>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;
    }

    renderStyleProperties(value) {
        return `
            <div class="style-editor">
                <textarea class="form-control form-control-sm" rows="4" 
                          placeholder="CSS styles...">${value}</textarea>
            </div>
        `;
    }

    renderFormattingRules(value) {
        return `
            <div class="formatting-rules">
                <textarea class="form-control form-control-sm" rows="4" 
                          placeholder="Formatting rules...">${value}</textarea>
            </div>
        `;
    }

    renderScriptProperties(value) {
        return `
            <div class="script-editor">
                <textarea class="form-control form-control-sm" rows="4" 
                          placeholder="JavaScript code...">${value}</textarea>
            </div>
        `;
    }

    renderSummaryProperties(value) {
        const summary = value ? JSON.parse(value) : { function: 'Sum', expression: '' };
        return `
            <div class="row g-2">
                <div class="col-6">
                    <label class="form-label small">Function</label>
                    <select class="form-select form-select-sm" onchange="updateSummaryProperty('function', this.value)">
                        <option value="Sum" ${summary.function === 'Sum' ? 'selected' : ''}>Sum</option>
                        <option value="Count" ${summary.function === 'Count' ? 'selected' : ''}>Count</option>
                        <option value="Average" ${summary.function === 'Average' ? 'selected' : ''}>Average</option>
                        <option value="Min" ${summary.function === 'Min' ? 'selected' : ''}>Min</option>
                        <option value="Max" ${summary.function === 'Max' ? 'selected' : ''}>Max</option>
                    </select>
                </div>
                <div class="col-6">
                    <label class="form-label small">Expression</label>
                    <input type="text" class="form-control form-control-sm" value="${summary.expression}" 
                           onchange="updateSummaryProperty('expression', this.value)">
                </div>
            </div>
        `;
    }

    renderGroupByProperties(value) {
        return `
            <div class="group-by">
                <textarea class="form-control form-control-sm" rows="3" 
                          placeholder="Group by fields...">${value}</textarea>
            </div>
        `;
    }

    renderSortByProperties(value) {
        return `
            <div class="sort-by">
                <textarea class="form-control form-control-sm" rows="3" 
                          placeholder="Sort by fields...">${value}</textarea>
            </div>
        `;
    }

    renderLineProperties(value) {
        return `
            <div class="line-properties">
                <textarea class="form-control form-control-sm" rows="3" 
                          placeholder="Line properties...">${value}</textarea>
            </div>
        `;
    }
}

// Global functions for property updates
function updateProperty(propertyName, value) {
    if (window.fabricCanvas && window.fabricCanvas.selectedObject) {
        window.fabricCanvas.selectedObject.set(propertyName, value);
        window.fabricCanvas.canvas.renderAll();
    }
}

function updateCoordinate(propertyName, index, value) {
    if (window.fabricCanvas && window.fabricCanvas.selectedObject) {
        const currentValue = window.fabricCanvas.selectedObject[propertyName] || '0, 0';
        const coords = currentValue.split(', ');
        coords[index] = value;
        window.fabricCanvas.selectedObject.set(propertyName, coords.join(', '));
        window.fabricCanvas.canvas.renderAll();
    }
}

function toggleCategory(categoryName) {
    const content = document.getElementById(`${categoryName}Content`);
    const toggle = content.previousElementSibling.querySelector('.category-toggle');
    
    if (content.style.display === 'none') {
        content.style.display = 'block';
        toggle.textContent = '-';
    } else {
        content.style.display = 'none';
        toggle.textContent = '+';
    }
}

// Additional property update functions
function updateFontProperty(property, value) {
    if (window.fabricCanvas && window.fabricCanvas.selectedObject) {
        const currentFont = window.fabricCanvas.selectedObject.font || 'Arial, 12pt';
        const fontObj = currentFont.includes('{') ? JSON.parse(currentFont) : { family: 'Arial', size: 12, weight: 'normal', style: 'normal' };
        fontObj[property] = value;
        window.fabricCanvas.selectedObject.set('font', JSON.stringify(fontObj));
        window.fabricCanvas.canvas.renderAll();
    }
}

function updatePaddingProperty(index, value) {
    if (window.fabricCanvas && window.fabricCanvas.selectedObject) {
        const currentPadding = window.fabricCanvas.selectedObject.padding || '0, 0, 0, 0';
        const padding = currentPadding.split(', ');
        padding[index] = value;
        window.fabricCanvas.selectedObject.set('padding', padding.join(', '));
        window.fabricCanvas.canvas.renderAll();
    }
}

function updateSummaryProperty(property, value) {
    if (window.fabricCanvas && window.fabricCanvas.selectedObject) {
        const currentSummary = window.fabricCanvas.selectedObject.summary || '{"function":"Sum","expression":""}';
        const summary = JSON.parse(currentSummary);
        summary[property] = value;
        window.fabricCanvas.selectedObject.set('summary', JSON.stringify(summary));
        window.fabricCanvas.canvas.renderAll();
    }
}

function addDataBinding() {
    // Implementation for adding new data binding
    console.log('Add data binding clicked');
}
