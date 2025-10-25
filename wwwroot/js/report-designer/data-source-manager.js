class DataSourceManager {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.dataSources = [];
        this.selectedSources = new Set();
    }

    async loadDataSources() {
        try {
            const response = await fetch('/Reports/Designer?handler=DataSources');
            if (!response.ok) {
                throw new Error('Failed to load data sources');
            }
            
            this.dataSources = await response.json();
            this.renderDataSources();
        } catch (error) {
            console.error('Error loading data sources:', error);
            this.showError('Failed to load data sources');
        }
    }

    renderDataSources() {
        if (this.dataSources.length === 0) {
            this.container.innerHTML = `
                <div class="text-center p-3">
                    <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                    <div class="text-muted">No data sources found</div>
                </div>
            `;
            return;
        }

        let html = '<div class="data-sources-list">';
        
        // Group by type
        const tables = this.dataSources.filter(ds => ds.type === 'table');
        const views = this.dataSources.filter(ds => ds.type === 'view');
        
        if (tables.length > 0) {
            html += this.renderDataSourceGroup('Tables', tables);
        }
        
        if (views.length > 0) {
            html += this.renderDataSourceGroup('Views', views);
        }
        
        html += '</div>';
        this.container.innerHTML = html;
        
        this.setupEventHandlers();
    }

    renderDataSourceGroup(title, sources) {
        let html = `
            <div class="data-source-group">
                <div class="group-header" onclick="toggleDataSourceGroup('${title}')">
                    <i class="fas fa-chevron-right group-toggle"></i>
                    <span class="group-title">${title}</span>
                    <span class="badge bg-secondary">${sources.length}</span>
                </div>
                <div class="group-content" id="${title}Content" style="display: none;">
        `;
        
        sources.forEach(source => {
            html += this.renderDataSource(source);
        });
        
        html += `
                </div>
            </div>
        `;
        
        return html;
    }

    renderDataSource(source) {
        const isSelected = this.selectedSources.has(source.name);
        const fieldCount = source.fields ? source.fields.length : 0;
        
        return `
            <div class="data-source-item ${isSelected ? 'selected' : ''}" 
                 data-source-name="${source.name}" 
                 data-source-type="${source.type}">
                <div class="data-source-header" onclick="toggleDataSource('${source.name}')">
                    <div class="d-flex align-items-center">
                        <i class="fas fa-${source.type === 'table' ? 'table' : 'eye'} me-2"></i>
                        <span class="source-name">${source.displayName}</span>
                        <span class="badge bg-light text-dark ms-auto">${fieldCount}</span>
                    </div>
                    <i class="fas fa-chevron-down source-toggle"></i>
                </div>
                <div class="data-source-fields" id="fields_${source.name}" style="display: none;">
                    <div class="fields-list">
                        ${this.renderFields(source.fields || [])}
                    </div>
                    <div class="data-source-actions p-2 border-top">
                        <button class="btn btn-sm btn-primary w-100 add-source-btn" 
                                data-source-name="${source.name}" 
                                data-source-fields='${JSON.stringify(source.fields || [])}'>
                            <i class="fas fa-plus me-1"></i> Add to Canvas
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    renderFields(fields) {
        if (fields.length === 0) {
            return '<div class="text-muted small p-2">No fields available</div>';
        }
        
        let html = '';
        fields.forEach(field => {
            html += `
                <div class="field-item" 
                     data-field-name="${field.name}" 
                     data-field-type="${field.dataType}"
                     draggable="true">
                    <div class="d-flex align-items-center">
                        <i class="fas fa-${this.getFieldIcon(field.dataType)} me-2 text-muted"></i>
                        <span class="field-name">${field.name}</span>
                        <span class="field-type text-muted small ms-auto">${field.dataType}</span>
                    </div>
                </div>
            `;
        });
        
        return html;
    }

    getFieldIcon(dataType) {
        const typeIcons = {
            'int': 'hashtag',
            'bigint': 'hashtag',
            'smallint': 'hashtag',
            'tinyint': 'hashtag',
            'decimal': 'hashtag',
            'numeric': 'hashtag',
            'float': 'hashtag',
            'real': 'hashtag',
            'money': 'dollar-sign',
            'smallmoney': 'dollar-sign',
            'varchar': 'text-width',
            'nvarchar': 'text-width',
            'char': 'text-width',
            'nchar': 'text-width',
            'text': 'text-width',
            'ntext': 'text-width',
            'date': 'calendar',
            'datetime': 'calendar',
            'datetime2': 'calendar',
            'smalldatetime': 'calendar',
            'time': 'clock',
            'bit': 'toggle-on',
            'uniqueidentifier': 'key',
            'binary': 'lock',
            'varbinary': 'lock',
            'image': 'image'
        };
        
        return typeIcons[dataType.toLowerCase()] || 'circle';
    }

    setupEventHandlers() {
        // Data source selection
        this.container.addEventListener('click', (e) => {
            const sourceItem = e.target.closest('.data-source-item');
            if (sourceItem && !e.target.closest('.data-source-header')) {
                this.toggleDataSourceSelection(sourceItem);
            }
        });

        // Add to Canvas button
        this.container.addEventListener('click', (e) => {
            if (e.target.closest('.add-source-btn')) {
                e.preventDefault();
                e.stopPropagation();
                
                const btn = e.target.closest('.add-source-btn');
                const sourceName = btn.dataset.sourceName;
                const sourceFields = JSON.parse(btn.dataset.sourceFields);
                
                if (window.fabricCanvas) {
                    window.fabricCanvas.addDataSourceFromPanel(sourceName, sourceFields);
                }
            }
        });

        // Field drag and drop
        this.container.addEventListener('dragstart', (e) => {
            if (e.target.classList.contains('field-item')) {
                const fieldItem = e.target.closest('.field-item');
                const sourceItem = fieldItem.closest('.data-source-item');
                
                e.dataTransfer.setData('text/plain', JSON.stringify({
                    type: 'field',
                    fieldName: fieldItem.dataset.fieldName,
                    fieldType: fieldItem.dataset.fieldType,
                    sourceName: sourceItem.dataset.sourceName
                }));
                
                fieldItem.classList.add('dragging');
            }
        });

        this.container.addEventListener('dragend', (e) => {
            if (e.target.classList.contains('field-item')) {
                e.target.classList.remove('dragging');
            }
        });
    }

    toggleDataSourceSelection(sourceItem) {
        const sourceName = sourceItem.dataset.sourceName;
        
        if (this.selectedSources.has(sourceName)) {
            this.selectedSources.delete(sourceName);
            sourceItem.classList.remove('selected');
        } else {
            this.selectedSources.add(sourceName);
            sourceItem.classList.add('selected');
        }
    }

    showError(message) {
        this.container.innerHTML = `
            <div class="text-center p-3">
                <i class="fas fa-exclamation-triangle text-danger fa-2x mb-2"></i>
                <div class="text-danger">${message}</div>
            </div>
        `;
    }

    getSelectedSources() {
        return Array.from(this.selectedSources);
    }

    getDataSourceByName(name) {
        return this.dataSources.find(ds => ds.name === name);
    }
}

// Global functions for data source interactions
function toggleDataSourceGroup(groupName) {
    const content = document.getElementById(`${groupName}Content`);
    const toggle = content.previousElementSibling.querySelector('.group-toggle');
    
    if (content.style.display === 'none') {
        content.style.display = 'block';
        toggle.classList.remove('fa-chevron-right');
        toggle.classList.add('fa-chevron-down');
    } else {
        content.style.display = 'none';
        toggle.classList.remove('fa-chevron-down');
        toggle.classList.add('fa-chevron-right');
    }
}

function toggleDataSource(sourceName) {
    const fieldsDiv = document.getElementById(`fields_${sourceName}`);
    const toggle = fieldsDiv.previousElementSibling.querySelector('.source-toggle');
    
    if (fieldsDiv.style.display === 'none') {
        fieldsDiv.style.display = 'block';
        toggle.classList.remove('fa-chevron-right');
        toggle.classList.add('fa-chevron-down');
    } else {
        fieldsDiv.style.display = 'none';
        toggle.classList.remove('fa-chevron-down');
        toggle.classList.add('fa-chevron-right');
    }
}
