/**
 * Excel-Like Table System
 * Phase 1: Foundation - Basic grid rendering
 * Phase 2: Cell editing - Excel-like keyboard shortcuts
 * Phase 3: Row/Column Management - Add/remove rows and columns
 * Phase 4: Basic Formula Engine - Formula parsing and evaluation
 * Phase 5: Formatting & Styling - Cell formatting with dialog
 * Phase 6: Column/Row Resizing - Drag handles for variable sizes
 */

class ExcelTable {
    constructor(options) {
        this.id = options.id || this.generateId();
        this.position = options.position || { x: 100, y: 100 };
        this.numColumns = options.columns || 5;
        this.numRows = options.rows || 10;
        this.cellWidth = options.cellWidth || 120;
        this.cellHeight = options.cellHeight || 24;
        
        // Phase 7: Initialize data - use provided data or initialize empty
        if (options.data && Array.isArray(options.data)) {
            this.data = options.data;
            this.numRows = options.data.length;
            this.numColumns = options.data[0] ? options.data[0].length : this.numColumns;
        } else {
            this.data = this.initializeData();
        }
        
        // DOM elements
        this.container = null;
        this.table = null;
        this.contextMenu = null;
        
        // State
        this.selectedCell = null;
        this.editingCell = null;
        this.contextMenuCell = null;
        
        // Formula engine (Phase 4)
        this.calculatedValues = {}; // Cache for formula results
        
        // Resizing (Phase 6)
        this.columnWidths = options.columnWidths || new Array(this.numColumns).fill(this.cellWidth);
        this.rowHeights = options.rowHeights || new Array(this.numRows).fill(this.cellHeight);
        this.resizing = false; // Track if currently resizing
        // Data-binding state (MVP)
        this.lastQuery = options.lastQuery || null;
        
        // Initialize
        this.render();
    }
    
    /**
     * Initialize empty data structure
     */
    initializeData() {
        const data = [];
        
        for (let row = 0; row < this.numRows; row++) {
            const rowData = [];
            for (let col = 0; col < this.numColumns; col++) {
                rowData.push({
                    value: '',
                    formula: null,
                    format: {
                        fontSize: 11,
                        fontFamily: 'Segoe UI, Arial, sans-serif',
                        fontWeight: 'normal',
                        textColor: '#000000',
                        backgroundColor: '#FFFFFF',
                        textAlign: 'left'
                    },
                    col: col,
                    row: row,
                    cellId: this.getCellId(col, row)
                });
            }
            data.push(rowData);
        }
        
        return data;
    }
    
    /**
     * Get Excel-style cell ID (A1, B2, etc.)
     */
    getCellId(col, row) {
        const colLetter = this.getColumnLetter(col);
        return colLetter + (row + 1);
    }
    
    /**
     * Convert column index to letter (0->A, 1->B, etc.)
     */
    getColumnLetter(col) {
        let letter = '';
        let colNum = col;
        
        while (colNum >= 0) {
            letter = String.fromCharCode(65 + (colNum % 26)) + letter;
            colNum = Math.floor(colNum / 26) - 1;
        }
        
        return letter;
    }
    
    /**
     * Render the table
     */
    render() {
        // Create container
        this.container = document.createElement('div');
        this.container.className = 'excel-table-container';
        this.container.id = 'excel-table-' + this.id;
        this.container.style.left = this.position.x + 'px';
        this.container.style.top = this.position.y + 'px';
        this.container.style.width = (this.numColumns * this.cellWidth + 40) + 'px';
        this.container.style.height = (this.numRows * this.cellHeight + 24) + 'px';
        
        // Add drag handle
        const dragHandle = document.createElement('div');
        dragHandle.className = 'excel-table-drag-handle';
        dragHandle.style.cssText = `
            position: absolute;
            top: -20px;
            left: 0;
            width: 100%;
            height: 20px;
            background: #007bff;
            color: white;
            text-align: center;
            line-height: 20px;
            cursor: move;
            font-size: 10px;
            z-index: 1000;
        `;
        dragHandle.textContent = '≡ Drag to Move';
        this.container.appendChild(dragHandle);
        
        // Setup drag functionality
        this.setupDrag(dragHandle);
        
        // Create table
        this.table = document.createElement('table');
        this.table.className = 'excel-table';
        
        // Create header row
        const headerRow = this.createHeaderRow();
        this.table.appendChild(headerRow);
        
        // Create data rows
        for (let row = 0; row < this.numRows; row++) {
            const tableRow = this.createDataRow(row);
            this.table.appendChild(tableRow);
        }
        
        this.container.appendChild(this.table);
        
        // Add to page
        const canvasContainer = document.querySelector('.canvas-wrapper');
        if (canvasContainer) {
            canvasContainer.appendChild(this.container);
        }
        
        // Setup event handlers
        this.setupEventHandlers();
    }
    
    /**
     * Create header row with column letters
     */
    createHeaderRow() {
        const headerRow = document.createElement('tr');
        
        // Empty corner cell
        const cornerCell = document.createElement('td');
        cornerCell.className = 'corner-cell';
        cornerCell.style.width = '40px'; // Fixed width for corner cell
        cornerCell.style.minWidth = '40px';
        cornerCell.style.maxWidth = '40px';
        headerRow.appendChild(cornerCell);
        
        // Column headers (A, B, C, ...) with resize handles (Phase 6)
        for (let col = 0; col < this.numColumns; col++) {
            const cell = document.createElement('td');
            cell.className = 'col-header';
            cell.textContent = this.getColumnLetter(col);
            cell.style.width = this.columnWidths[col] + 'px';
            cell.style.position = 'relative';
            headerRow.appendChild(cell);
            
            // Add resize handle
            const resizeHandle = document.createElement('div');
            resizeHandle.className = 'col-resize-handle';
            resizeHandle.style.cssText = `
                position: absolute;
                right: -3px;
                top: 0;
                width: 6px;
                height: 100%;
                cursor: col-resize;
                z-index: 10;
                background: transparent;
            `;
            cell.appendChild(resizeHandle);
            
            // Add resize listeners
            this.setupColumnResize(resizeHandle, col);
        }
        
        return headerRow;
    }
    
    /**
     * Create data row
     */
    createDataRow(row) {
        const tableRow = document.createElement('tr');
        
        // Row number cell with resize handle (Phase 6)
        const rowNumberCell = document.createElement('td');
        rowNumberCell.className = 'row-number';
        rowNumberCell.textContent = row + 1;
        rowNumberCell.style.width = '40px'; // Fixed width for row numbers
        rowNumberCell.style.minWidth = '40px';
        rowNumberCell.style.maxWidth = '40px';
        rowNumberCell.style.height = this.rowHeights[row] + 'px';
        rowNumberCell.style.position = 'relative';
        tableRow.appendChild(rowNumberCell);
        
        // Add resize handle
        const rowResizeHandle = document.createElement('div');
        rowResizeHandle.className = 'row-resize-handle';
        rowResizeHandle.style.cssText = `
            position: absolute;
            bottom: -3px;
            left: 0;
            width: 100%;
            height: 6px;
            cursor: row-resize;
            z-index: 10;
            background: transparent;
        `;
        rowNumberCell.appendChild(rowResizeHandle);
        
        // Add resize listeners
        this.setupRowResize(rowResizeHandle, row);
        
        // Data cells
        for (let col = 0; col < this.numColumns; col++) {
            const cell = this.createDataCell(row, col);
            tableRow.appendChild(cell);
        }
        
        return tableRow;
    }
    
    /**
     * Create individual data cell
     */
    createDataCell(row, col) {
        const cell = document.createElement('td');
        cell.className = 'data-cell';
        cell.dataset.row = row;
        cell.dataset.col = col;
        cell.dataset.cellId = this.getCellId(col, row);
        
        // Apply variable widths and heights (Phase 6)
        if (this.columnWidths && this.columnWidths[col]) {
            cell.style.width = this.columnWidths[col] + 'px';
        }
        if (this.rowHeights && this.rowHeights[row]) {
            cell.style.height = this.rowHeights[row] + 'px';
        }
        
        const cellData = this.data[row][col];
        
        // Apply cell formatting (Phase 5)
        this.applyCellFormatting(cell, cellData.format);
        
        // Create content element
        const content = document.createElement('div');
        content.className = 'cell-content';
        
        // Display calculated value for formulas, regular value otherwise (Phase 4)
        if (cellData.formula) {
            const result = this.evaluateFormula(cellData.formula, row, col);
            content.textContent = result.toString();
            this.calculatedValues[this.getCellId(col, row)] = result;
        } else {
            content.textContent = cellData.value;
        }
        
        content.contentEditable = false;
        
        cell.appendChild(content);
        
        return cell;
    }
    
    /**
     * Setup event handlers
     */
    setupEventHandlers() {
        // Click to select and immediately enter edit mode
        this.container.addEventListener('click', (e) => {
            const cell = e.target.closest('.data-cell');
            if (cell && !cell.classList.contains('editing')) {
                this.selectAndEditCell(cell);
            }
        });
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (this.editingCell) {
                // Already handled in startEditing
                return;
            }
            
            // F2 to edit selected cell
            if (e.key === 'F2' && this.selectedCell) {
                e.preventDefault();
                this.startEditing(this.selectedCell);
            }
            
            // Arrow keys to move selection and enter edit mode (only when not already editing)
            if (e.key.startsWith('Arrow') && this.selectedCell && !this.editingCell) {
                e.preventDefault();
                this.moveSelectionAndEdit(e.key.replace('Arrow', ''));
            }
        });
        
        // Right-click for context menu
        this.container.addEventListener('contextmenu', (e) => {
            const cell = e.target.closest('.data-cell');
            if (cell) {
                e.preventDefault();
                this.showContextMenu(e, cell);
            }
        });
    }
    
    /**
     * Setup drag functionality
     */
    setupDrag(dragHandle) {
        let isDragging = false;
        let startX = 0;
        let startY = 0;
        let startLeft = 0;
        let startTop = 0;
        
        dragHandle.addEventListener('mousedown', (e) => {
            isDragging = true;
            startX = e.clientX;
            startY = e.clientY;
            startLeft = this.container.offsetLeft;
            startTop = this.container.offsetTop;
            
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
            
            e.preventDefault();
        });
        
        const onMouseMove = (e) => {
            if (!isDragging) return;
            
            const diffX = e.clientX - startX;
            const diffY = e.clientY - startY;
            
            const newLeft = startLeft + diffX;
            const newTop = startTop + diffY;
            
            this.container.style.left = newLeft + 'px';
            this.container.style.top = newTop + 'px';
            
            this.position = { x: newLeft, y: newTop };
        };
        
        const onMouseUp = (e) => {
            isDragging = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };
    }
    
    /**
     * Select a cell
     */
    selectCell(cellElement) {
        // Deselect previous
        if (this.selectedCell) {
            this.selectedCell.classList.remove('selected');
        }
        
        // Select new
        this.selectedCell = cellElement;
        cellElement.classList.add('selected');
    }
    
    /**
     * Select and immediately enter edit mode (single click)
     */
    selectAndEditCell(cellElement) {
        this.selectCell(cellElement);
        
        // Small delay to ensure selection is visible, then start editing
        setTimeout(() => {
            if (this.selectedCell === cellElement) {
                this.startEditing(cellElement);
            }
        }, 50);
    }
    
    /**
     * Start editing a cell
     */
    startEditing(cellElement) {
        if (this.editingCell) {
            this.stopEditing();
        }
        
        cellElement.classList.add('editing');
        this.editingCell = cellElement;
        
        const content = cellElement.querySelector('.cell-content');
        if (content) {
            // Show formula when editing (Phase 4)
            const row = parseInt(cellElement.dataset.row);
            const col = parseInt(cellElement.dataset.col);
            const cellData = this.data[row][col];
            
            if (cellData.formula) {
                content.textContent = cellData.formula; // Show formula, not result
            }
            
            content.contentEditable = true;
            content.focus();
            
            // Move cursor to end
            const range = document.createRange();
            range.selectNodeContents(content);
            range.collapse(false); // Move to end
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
        }
        
        // Single-use key handler for this edit session
        const keyHandler = (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                // Normal Enter - move down
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.stopEditing();
                setTimeout(() => this.moveSelectionAndEdit('Down'), 0);
            } else if (e.key === 'Enter' && e.shiftKey) {
                // Shift+Enter - move up
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.stopEditing();
                setTimeout(() => this.moveSelectionAndEdit('Up'), 0);
            } else if (e.key === 'Escape') {
                e.preventDefault();
                this.cancelEditing();
            } else if (e.key === 'Tab') {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.stopEditing();
                setTimeout(() => this.moveSelectionAndEdit(e.shiftKey ? 'Left' : 'Right'), 0);
            } else if (e.key.startsWith('Arrow') && (e.ctrlKey || e.metaKey)) {
                // Ctrl+Arrow or Cmd+Arrow - move selection without editing
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                this.stopEditing();
                const direction = e.key.replace('Arrow', '');
                this.moveSelection(direction);
            }
        };
        
        content.addEventListener('keydown', keyHandler);
        
        // Store handler reference on the cell element
        cellElement._keyHandler = keyHandler;
    }
    
    /**
     * Stop editing and save (Phase 4: Enhanced with formula support)
     */
    stopEditing() {
        if (!this.editingCell) return;
        
        const content = this.editingCell.querySelector('.cell-content');
        if (content) {
            const row = parseInt(this.editingCell.dataset.row);
            const col = parseInt(this.editingCell.dataset.col);
            const rawValue = content.textContent.trim();
            
            // Check if it's a formula (starts with "=")
            if (rawValue.startsWith('=')) {
                // Store as formula
                this.data[row][col].formula = rawValue;
                this.data[row][col].value = ''; // Raw value is empty for formulas
                
                // Evaluate formula and cache result
                const result = this.evaluateFormula(rawValue, row, col);
                
                // Display the result
                content.textContent = result.toString();
                
                // Store calculated value
                this.calculatedValues[this.getCellId(col, row)] = result;
            } else {
                // Regular value
                this.data[row][col].value = rawValue;
                this.data[row][col].formula = null;
                
                // Delete cached value
                delete this.calculatedValues[this.getCellId(col, row)];
                
                // Display the value
                content.textContent = rawValue;
            }
            
            // Remove event handler
            const keyHandler = this.editingCell._keyHandler;
            if (keyHandler) {
                content.removeEventListener('keydown', keyHandler);
                delete this.editingCell._keyHandler;
            }
            
            content.contentEditable = false;
        }
        
        this.editingCell.classList.remove('editing');
        this.editingCell = null;
    }
    
    /**
     * Cancel editing without saving
     */
    cancelEditing() {
        if (!this.editingCell) return;
        
        const content = this.editingCell.querySelector('.cell-content');
        if (content) {
            // Restore original value (formula or regular value)
            const row = parseInt(this.editingCell.dataset.row);
            const col = parseInt(this.editingCell.dataset.col);
            const cellData = this.data[row][col];
            
            // Show formula if it exists, otherwise show value
            if (cellData.formula) {
                content.textContent = cellData.formula;
            } else {
                content.textContent = cellData.value;
            }
            
            // Remove event handler
            const keyHandler = this.editingCell._keyHandler;
            if (keyHandler) {
                content.removeEventListener('keydown', keyHandler);
                delete this.editingCell._keyHandler;
            }
            
            content.contentEditable = false;
        }
        
        this.editingCell.classList.remove('editing');
        this.editingCell = null;
    }
    
    /**
     * Move to next cell (Tab key)
     */
    moveToNextCell() {
        if (!this.selectedCell) return;
        
        const row = parseInt(this.selectedCell.dataset.row);
        const col = parseInt(this.selectedCell.dataset.col);
        
        let nextCol = col + 1;
        let nextRow = row;
        
        // Wrap to next row if at end
        if (nextCol >= this.numColumns) {
            nextCol = 0;
            nextRow = row + 1;
            
            // Wrap to top if at end
            if (nextRow >= this.numRows) {
                nextRow = 0;
            }
        }
        
        const nextCell = this.container.querySelector(
            `.data-cell[data-row="${nextRow}"][data-col="${nextCol}"]`
        );
        
        if (nextCell) {
            this.selectCell(nextCell);
        }
    }
    
    /**
     * Show context menu (Phase 3)
     */
    showContextMenu(e, cellElement) {
        // Hide any existing context menu
        this.hideContextMenu();
        
        // Store reference to clicked cell
        this.contextMenuCell = cellElement;
        
        const row = parseInt(cellElement.dataset.row);
        const col = parseInt(cellElement.dataset.col);
        
        // Create context menu
        const menu = document.createElement('div');
        menu.className = 'excel-context-menu';
        menu.style.position = 'fixed';
        menu.style.left = e.clientX + 'px';
        menu.style.top = e.clientY + 'px';
        
        // Build menu items
        const menuItems = [];

        // Data binding (MVP)
        menuItems.push({ text: '🔗 Get Data (SQL)...', action: () => this.showGetDataDialog() });
        menuItems.push({ text: '---', separator: true });
        
        // Formatting options (Phase 5)
        menuItems.push({ text: '📝 Format Cell...', action: () => this.showFormatDialog(row, col) });
        menuItems.push({ text: '---', separator: true });
        
        // Row operations
        menuItems.push({ text: 'Insert Row Above', action: () => this.insertRow(row, 'above') });
        menuItems.push({ text: 'Insert Row Below', action: () => this.insertRow(row, 'below') });
        
        // Only allow delete if more than 1 row
        if (this.numRows > 1) {
            menuItems.push({ text: 'Delete Row', action: () => this.deleteRow(row) });
        }
        
        menuItems.push({ text: '---', separator: true });
        
        // Column operations
        menuItems.push({ text: 'Insert Column Left', action: () => this.insertColumn(col, 'left') });
        menuItems.push({ text: 'Insert Column Right', action: () => this.insertColumn(col, 'right') });
        
        // Only allow delete if more than 1 column
        if (this.numColumns > 1) {
            menuItems.push({ text: 'Delete Column', action: () => this.deleteColumn(col) });
        }
        
        // Create menu items
        menuItems.forEach(item => {
            if (item.separator) {
                const sep = document.createElement('div');
                sep.className = 'excel-context-menu-separator';
                menu.appendChild(sep);
            } else {
                const menuItem = document.createElement('div');
                menuItem.className = 'excel-context-menu-item';
                menuItem.textContent = item.text;
                menuItem.addEventListener('click', () => {
                    item.action();
                    this.hideContextMenu();
                });
                menu.appendChild(menuItem);
            }
        });
        
        this.contextMenu = menu;
        document.body.appendChild(menu);
        
        // Close menu when clicking elsewhere
        setTimeout(() => {
            document.addEventListener('click', this.hideContextMenu.bind(this), { once: true });
        }, 0);
    }

    /**
     * Show simple Get Data dialog (SQL only, MVP)
     */
    showGetDataDialog() {
        const overlay = document.createElement('div');
        overlay.style.cssText = `
            position: fixed; inset: 0; background: rgba(0,0,0,.45);
            z-index: 10002; display: flex; align-items: center; justify-content: center;
        `;

        const dialog = document.createElement('div');
        dialog.style.cssText = `
            width: 640px; max-width: 90vw; background: #fff; border-radius: 8px;
            box-shadow: 0 10px 30px rgba(0,0,0,.25); padding: 16px; font-family: Segoe UI, Arial;
        `;
        dialog.innerHTML = `
            <h3 style="margin: 0 0 8px 0;">Get Data (SQL)</h3>
            <div style="margin-bottom: 8px; color: #555; font-size: 12px;">
              Only SELECT is allowed. Preview limited to 100 rows.
            </div>
            <div style="display:flex; gap:12px;">
              <div style="flex:1;">
                <div style="display:flex; gap:8px; align-items:center; margin-bottom:6px;">
                  <strong>Builder</strong>
                  <button id="gd-load-schema" style="padding:4px 8px; border:1px solid #ccc; background:#f8f9fa; border-radius:4px;">Load Schema</button>
                </div>
                <div style="display:grid; grid-template-columns: 1fr 1fr; gap:8px;">
                  <div>
                    <label style="font-size:12px; color:#333;">Table</label>
                    <select id="gd-table" style="width:100%; padding:6px; border:1px solid #ddd; border-radius:4px;">
                      <option value="">-- select --</option>
                    </select>
                  </div>
                  <div>
                    <label style="font-size:12px; color:#333;">Columns</label>
                    <div id="gd-columns" style="height:120px; overflow:auto; border:1px solid #ddd; border-radius:4px; padding:6px; background:#fff;"></div>
                  </div>
                </div>
                <div style="margin-top:8px;">
                  <button id="gd-generate" style="padding:6px 10px; border:none; background:#6c757d; color:#fff; border-radius:4px;">Generate SQL</button>
                </div>
              </div>
              <div style="flex:1;">
                <label style="display:block; font-weight:bold; margin-bottom:4px;">SQL</label>
                <textarea id="gd-sql" style="width:100%; height:220px; font-family: Consolas, monospace; font-size:12px; padding:8px; border:1px solid #ddd; border-radius:4px;" placeholder="SELECT ..."></textarea>
              </div>
            </div>
            <div id="gd-error" style="margin-top:8px; color:#b00020; display:none;"></div>
            <div id="gd-preview" style="margin-top:12px; max-height:240px; overflow:auto; border:1px solid #eee; display:none;"></div>
            <div style="display:flex; gap:8px; justify-content:flex-end; margin-top:12px;">
              <button id="gd-cancel" style="padding:6px 12px; border:1px solid #ccc; background:#f5f5f5; border-radius:4px;">Cancel</button>
              <button id="gd-preview-btn" style="padding:6px 12px; border:none; background:#0d6efd; color:#fff; border-radius:4px;">Preview</button>
              <button id="gd-bind" style="padding:6px 12px; border:none; background:#198754; color:#fff; border-radius:4px; display:none;">Bind to Table</button>
            </div>
        `;

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        const $sql = dialog.querySelector('#gd-sql');
        const $err = dialog.querySelector('#gd-error');
        const $prev = dialog.querySelector('#gd-preview');
        const $btnPrev = dialog.querySelector('#gd-preview-btn');
        const $btnBind = dialog.querySelector('#gd-bind');
        const $btnCancel = dialog.querySelector('#gd-cancel');
        const $loadSchema = dialog.querySelector('#gd-load-schema');
        const $table = dialog.querySelector('#gd-table');
        const $columns = dialog.querySelector('#gd-columns');
        const $generate = dialog.querySelector('#gd-generate');

        let lastPreview = { columns: [], rows: [] };
        let schemaItems = [];

        const close = () => { document.body.removeChild(overlay); };

        $btnCancel.addEventListener('click', close);

        $loadSchema.addEventListener('click', async () => {
            $columns.innerHTML = '';
            $table.innerHTML = '<option value="">-- select --</option>';
            try {
                const res = await fetch('/api/ReportWizard/data/schema');
                const json = await res.json();
                if (!json.success) throw new Error(json.error || 'Failed to load schema');
                schemaItems = json.items || [];
                schemaItems.forEach(item => {
                    const opt = document.createElement('option');
                    opt.value = `${item.schema}.${item.table}`;
                    opt.textContent = `${item.schema}.${item.table}`;
                    $table.appendChild(opt);
                });
            } catch (err) {
                $err.textContent = (err && err.message) ? err.message : 'Unknown error';
                $err.style.display = 'block';
            }
        });

        $table.addEventListener('change', () => {
            $columns.innerHTML = '';
            const v = $table.value;
            if (!v) return;
            const item = schemaItems.find(i => `${i.schema}.${i.table}` === v);
            if (!item) return;
            item.columns.forEach(col => {
                const id = `col_${item.schema}_${item.table}_${col.name}`;
                const wrap = document.createElement('div');
                wrap.style.marginBottom = '4px';
                wrap.innerHTML = `<label style="font-size:12px; display:flex; gap:6px; align-items:center;"><input type="checkbox" id="${id}" data-col="${col.name}"> <span>${col.name} <span style=\"color:#888;\">(${col.dataType})</span></span></label>`;
                $columns.appendChild(wrap);
            });
        });

        $generate.addEventListener('click', () => {
            $err.style.display = 'none';
            const v = $table.value;
            if (!v) {
                $err.textContent = 'Select a table.';
                $err.style.display = 'block';
                return;
            }
            const checked = Array.from($columns.querySelectorAll('input[type="checkbox"]:checked'))
                .map(i => i.getAttribute('data-col'));
            const colsSql = checked.length > 0 ? checked.map(c => `[${c}]`).join(', ') : '*';
            const [schema, table] = v.split('.');
            const tableSql = `[${schema}].[${table}]`;
            $sql.value = `SELECT ${colsSql} FROM ${tableSql}`;
        });

        $btnPrev.addEventListener('click', async () => {
            $err.style.display = 'none';
            $err.textContent = '';
            $prev.style.display = 'none';
            $prev.innerHTML = '';
            $btnBind.style.display = 'none';

            const sql = ($sql.value || '').trim();
            if (!sql.toLowerCase().startsWith('select')) {
                $err.textContent = 'Only SELECT statements are allowed.';
                $err.style.display = 'block';
                return;
            }

            try {
                const res = await fetch('/api/ReportWizard/data/preview-sql', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ sql, maxRows: 100 })
                });
                const json = await res.json();
                if (!json.success) {
                    throw new Error(json.error || 'Preview failed');
                }

                lastPreview = { columns: json.columns || [], rows: json.rows || [] };
                // Keep the SQL used for preview as the binding candidate
                this.lastQuery = sql;

                // Render a light preview table
                const tbl = document.createElement('table');
                tbl.style.cssText = 'width:100%; border-collapse: collapse; font-size:12px;';
                const thead = document.createElement('thead');
                const trh = document.createElement('tr');
                lastPreview.columns.forEach(c => {
                    const th = document.createElement('th');
                    th.textContent = c;
                    th.style.cssText = 'text-align:left; border-bottom:1px solid #ddd; padding:4px; position:sticky; top:0; background:#fafafa;';
                    trh.appendChild(th);
                });
                thead.appendChild(trh);
                tbl.appendChild(thead);
                const tbody = document.createElement('tbody');
                lastPreview.rows.forEach(r => {
                    const tr = document.createElement('tr');
                    r.forEach(v => {
                        const td = document.createElement('td');
                        td.textContent = v == null ? '' : String(v);
                        td.style.cssText = 'border-bottom:1px solid #f0f0f0; padding:4px;';
                        tr.appendChild(td);
                    });
                    tbody.appendChild(tr);
                });
                tbl.appendChild(tbody);

                $prev.innerHTML = '';
                $prev.appendChild(tbl);
                $prev.style.display = 'block';
                $btnBind.style.display = 'inline-block';
            } catch (err) {
                $err.textContent = (err && err.message) ? err.message : 'Unknown error';
                $err.style.display = 'block';
            }
        });

        $btnBind.addEventListener('click', () => {
            this.bindPreviewToTable(lastPreview);
            close();
        });
    }

    /**
     * Bind preview result to the current table (reset shape)
     */
    bindPreviewToTable(preview) {
        if (!preview || !Array.isArray(preview.rows)) return;

        const rows = preview.rows;
        const cols = preview.columns || (rows[0] ? rows[0].map((_, i) => 'Col' + (i + 1)) : []);

        // Resize table shape
        this.numRows = rows.length > 0 ? rows.length : 1;
        this.numColumns = cols.length > 0 ? cols.length : 1;

        // Build data model from rows
        const newData = [];
        for (let r = 0; r < this.numRows; r++) {
            const rowData = [];
            const src = rows[r] || [];
            for (let c = 0; c < this.numColumns; c++) {
                const val = src[c] == null ? '' : String(src[c]);
                rowData.push({
                    value: val,
                    formula: null,
                    format: {
                        fontSize: 11,
                        fontFamily: 'Segoe UI, Arial, sans-serif',
                        fontWeight: 'normal',
                        textColor: '#000000',
                        backgroundColor: '#FFFFFF',
                        textAlign: 'left'
                    },
                    col: c,
                    row: r,
                    cellId: this.getCellId(c, r)
                });
            }
            newData.push(rowData);
        }

        this.data = newData;
        this.columnWidths = new Array(this.numColumns).fill(this.cellWidth);
        this.rowHeights = new Array(this.numRows).fill(this.cellHeight);

        this.rerender();
    }

    /**
     * Refresh data by re-running the stored SQL (if any)
     */
    async refreshFromSql() {
        if (!this.lastQuery) return false;
        try {
            const res = await fetch('/api/ReportWizard/data/preview-sql', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ sql: this.lastQuery, maxRows: 1000 })
            });
            const json = await res.json();
            if (!json.success) return false;
            this.bindPreviewToTable({ columns: json.columns || [], rows: json.rows || [] });
            return true;
        } catch (_) {
            return false;
        }
    }
    
    /**
     * Hide context menu
     */
    hideContextMenu() {
        if (this.contextMenu) {
            this.contextMenu.remove();
            this.contextMenu = null;
        }
        this.contextMenuCell = null;
    }
    
    /**
     * Move selection with arrow keys (without entering edit mode)
     */
    moveSelection(direction) {
        if (!this.selectedCell) return;
        
        // First, make sure we're not in edit mode
        if (this.editingCell) {
            this.stopEditing();
        }
        
        const row = parseInt(this.selectedCell.dataset.row);
        const col = parseInt(this.selectedCell.dataset.col);
        
        let nextRow = row;
        let nextCol = col;
        
        switch(direction) {
            case 'Up':
                nextRow = Math.max(0, row - 1);
                break;
            case 'Down':
                nextRow = Math.min(this.numRows - 1, row + 1);
                break;
            case 'Left':
                nextCol = Math.max(0, col - 1);
                break;
            case 'Right':
                nextCol = Math.min(this.numColumns - 1, col + 1);
                break;
        }
        
        // Only move if we actually changed position
        if (nextRow !== row || nextCol !== col) {
            const nextCell = this.container.querySelector(
                `.data-cell[data-row="${nextRow}"][data-col="${nextCol}"]`
            );
            
            if (nextCell) {
                this.selectCell(nextCell);
                // Scroll into view if needed
                nextCell.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        }
    }
    
    /**
     * Move selection and automatically enter edit mode
     */
    moveSelectionAndEdit(direction) {
        if (!this.selectedCell) return;
        
        const row = parseInt(this.selectedCell.dataset.row);
        const col = parseInt(this.selectedCell.dataset.col);
        
        let nextRow = row;
        let nextCol = col;
        
        switch(direction) {
            case 'Up':
                nextRow = Math.max(0, row - 1);
                break;
            case 'Down':
                nextRow = Math.min(this.numRows - 1, row + 1);
                break;
            case 'Left':
                nextCol = Math.max(0, col - 1);
                break;
            case 'Right':
                nextCol = Math.min(this.numColumns - 1, col + 1);
                break;
        }
        
        const nextCell = this.container.querySelector(
            `.data-cell[data-row="${nextRow}"][data-col="${nextCol}"]`
        );
        
        if (nextCell) {
            this.selectCell(nextCell);
            // Scroll into view if needed
            nextCell.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            // Enter edit mode immediately
            setTimeout(() => this.startEditing(nextCell), 50);
        }
    }
    
    /**
     * Get table data as JSON
     */
    serialize() {
        return {
            id: this.id,
            position: this.position,
            columns: this.numColumns,
            rows: this.numRows,
            cellWidth: this.cellWidth,
            cellHeight: this.cellHeight,
            data: this.data,
            columnWidths: this.columnWidths, // Phase 6
            rowHeights: this.rowHeights // Phase 6
        };
    }
    
    /**
     * Load table data from JSON
     */
    deserialize(data) {
        this.id = data.id;
        this.position = data.position;
        this.numColumns = data.columns;
        this.numRows = data.rows;
        this.cellWidth = data.cellWidth;
        this.cellHeight = data.cellHeight;
        this.data = data.data;
        
        // Phase 6: Restore widths and heights
        this.columnWidths = data.columnWidths || new Array(this.numColumns).fill(this.cellWidth);
        this.rowHeights = data.rowHeights || new Array(this.numRows).fill(this.cellHeight);
        
        // Re-render
        if (this.container) {
            this.container.remove();
        }
        this.render();
    }
    
    /**
     * Generate unique ID
     */
    generateId() {
        return 'excel-table-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
    }
    
    /**
     * Phase 3: Row/Column Management
     */
    
    /**
     * Insert a new row above or below the specified row
     */
    insertRow(rowIndex, position) {
        const insertIndex = position === 'below' ? rowIndex + 1 : rowIndex;
        
        // Create new row data
        const newRow = [];
        for (let col = 0; col < this.numColumns; col++) {
            newRow.push({
                value: '',
                formula: null,
                format: {
                    fontSize: 11,
                    fontFamily: 'Segoe UI, Arial, sans-serif',
                    fontWeight: 'normal',
                    textColor: '#000000',
                    backgroundColor: '#FFFFFF',
                    textAlign: 'left'
                },
                col: col,
                row: insertIndex,
                cellId: this.getCellId(col, insertIndex)
            });
        }
        
        // Insert into data array
        this.data.splice(insertIndex, 0, newRow);
        
        // Update row numbers for all rows after inserted row
        for (let row = insertIndex + 1; row < this.data.length; row++) {
            for (let col = 0; col < this.numColumns; col++) {
                this.data[row][col].row = row;
                this.data[row][col].cellId = this.getCellId(col, row);
            }
        }
        
        this.numRows++;
        
        // Add row height to array (Phase 6)
        this.rowHeights.splice(insertIndex, 0, this.cellHeight);
        
        // Re-render the table
        this.rerender();
    }
    
    /**
     * Delete a row
     */
    deleteRow(rowIndex) {
        if (this.numRows <= 1) return; // Must keep at least 1 row
        
        // Remove from data array
        this.data.splice(rowIndex, 1);
        
        // Update row numbers
        for (let row = rowIndex; row < this.data.length; row++) {
            for (let col = 0; col < this.numColumns; col++) {
                this.data[row][col].row = row;
                this.data[row][col].cellId = this.getCellId(col, row);
            }
        }
        
        this.numRows--;
        
        // Remove row height from array (Phase 6)
        this.rowHeights.splice(rowIndex, 1);
        
        // Re-render the table
        this.rerender();
    }
    
    /**
     * Insert a new column left or right of the specified column
     */
    insertColumn(colIndex, position) {
        const insertIndex = position === 'right' ? colIndex + 1 : colIndex;
        
        // Insert a new column into each row
        for (let row = 0; row < this.numRows; row++) {
            const newCell = {
                value: '',
                formula: null,
                format: {
                    fontSize: 11,
                    fontFamily: 'Segoe UI, Arial, sans-serif',
                    fontWeight: 'normal',
                    textColor: '#000000',
                    backgroundColor: '#FFFFFF',
                    textAlign: 'left'
                },
                col: insertIndex,
                row: row,
                cellId: this.getCellId(insertIndex, row)
            };
            
            this.data[row].splice(insertIndex, 0, newCell);
        }
        
        // Update column indices for all cells in columns after inserted column
        for (let row = 0; row < this.numRows; row++) {
            for (let col = insertIndex + 1; col < this.numColumns + 1; col++) {
                this.data[row][col].col = col;
                this.data[row][col].cellId = this.getCellId(col, row);
            }
        }
        
        this.numColumns++;
        
        // Add column width to array (Phase 6)
        this.columnWidths.splice(insertIndex, 0, this.cellWidth);
        
        // Re-render the table
        this.rerender();
    }
    
    /**
     * Delete a column
     */
    deleteColumn(colIndex) {
        if (this.numColumns <= 1) return; // Must keep at least 1 column
        
        // Remove column from each row
        for (let row = 0; row < this.numRows; row++) {
            this.data[row].splice(colIndex, 1);
        }
        
        // Update column indices
        for (let row = 0; row < this.numRows; row++) {
            for (let col = colIndex; col < this.numColumns - 1; col++) {
                this.data[row][col].col = col;
                this.data[row][col].cellId = this.getCellId(col, row);
            }
        }
        
        this.numColumns--;
        
        // Remove column width from array (Phase 6)
        this.columnWidths.splice(colIndex, 1);
        
        // Re-render the table
        this.rerender();
    }
    
    /**
     * Re-render the entire table (after row/column operations)
     */
    rerender() {
        // Store current selection
        const selectedRow = this.selectedCell ? parseInt(this.selectedCell.dataset.row) : null;
        const selectedCol = this.selectedCell ? parseInt(this.selectedCell.dataset.col) : null;
        
        // Save current edit state if any
        if (this.editingCell) {
            this.stopEditing();
        }
        
        // Remove old table
        this.table.remove();
        
        // Recreate table
        this.table = document.createElement('table');
        this.table.className = 'excel-table';
        
        // Recreate header row
        const headerRow = this.createHeaderRow();
        this.table.appendChild(headerRow);
        
        // Recreate data rows
        for (let row = 0; row < this.numRows; row++) {
            const tableRow = this.createDataRow(row);
            this.table.appendChild(tableRow);
        }
        
        this.container.appendChild(this.table);
        
        // Update container size
        this.container.style.width = (this.numColumns * this.cellWidth + 40) + 'px';
        this.container.style.height = (this.numRows * this.cellHeight + 24) + 'px';
        
        // Restore selection if valid
        if (selectedRow !== null && selectedCol !== null && 
            selectedRow < this.numRows && selectedCol < this.numColumns) {
            const cell = this.container.querySelector(
                `.data-cell[data-row="${selectedRow}"][data-col="${selectedCol}"]`
            );
            if (cell) {
                this.selectCell(cell);
            }
        }
    }
    
    /**
     * Phase 5: Formatting & Styling
     */
    
    /**
     * Apply formatting to a cell element
     */
    applyCellFormatting(cell, format) {
        if (!format) return;
        
        const style = cell.style;
        
        // Apply font properties
        if (format.fontSize) {
            style.fontSize = format.fontSize + 'px';
        }
        if (format.fontFamily) {
            style.fontFamily = format.fontFamily;
        }
        if (format.fontWeight) {
            style.fontWeight = format.fontWeight;
        }
        if (format.textColor) {
            style.color = format.textColor;
        }
        if (format.backgroundColor) {
            style.backgroundColor = format.backgroundColor;
        }
        if (format.textAlign) {
            style.textAlign = format.textAlign;
        }
    }
    
    /**
     * Update formatting for a specific cell
     */
    updateCellFormat(row, col, formatChanges) {
        if (!this.data[row] || !this.data[row][col]) return;
        
        const cellData = this.data[row][col];
        const currentFormat = cellData.format;
        
        // Merge changes
        cellData.format = { ...currentFormat, ...formatChanges };
        
        // Update the DOM cell if it exists
        const cellElement = this.container.querySelector(
            `.data-cell[data-row="${row}"][data-col="${col}"]`
        );
        
        if (cellElement) {
            this.applyCellFormatting(cellElement, cellData.format);
            
            // Also update the content element
            const content = cellElement.querySelector('.cell-content');
            if (content && formatChanges.textColor) {
                content.style.color = formatChanges.textColor;
            }
            if (content && formatChanges.backgroundColor) {
                content.style.backgroundColor = formatChanges.backgroundColor;
            }
            if (content && formatChanges.textAlign) {
                content.style.textAlign = formatChanges.textAlign;
            }
        }
    }
    
    /**
     * Get formatting for a specific cell
     */
    getCellFormat(row, col) {
        if (!this.data[row] || !this.data[row][col]) return null;
        return this.data[row][col].format;
    }
    
    /**
     * Show format dialog for a cell (Phase 5)
     */
    showFormatDialog(row, col) {
        // Create dialog overlay
        const overlay = document.createElement('div');
        overlay.className = 'excel-format-overlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.5);
            z-index: 10001;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        
        // Create dialog
        const dialog = document.createElement('div');
        dialog.className = 'excel-format-dialog';
        dialog.style.cssText = `
            background: white;
            padding: 20px;
            border-radius: 8px;
            min-width: 300px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.3);
        `;
        
        // Get current format
        const currentFormat = this.getCellFormat(row, col);
        
        // Build dialog content
        dialog.innerHTML = `
            <h3 style="margin-top: 0;">Format Cell</h3>
            
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: bold;">Font Size:</label>
                <input type="number" id="format-fontSize" value="${currentFormat.fontSize || 11}" min="8" max="72" style="width: 100%; padding: 5px;">
            </div>
            
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: bold;">Text Color:</label>
                <input type="color" id="format-textColor" value="${currentFormat.textColor || '#000000'}" style="width: 100%; height: 40px;">
            </div>
            
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: bold;">Background Color:</label>
                <input type="color" id="format-backgroundColor" value="${currentFormat.backgroundColor || '#FFFFFF'}" style="width: 100%; height: 40px;">
            </div>
            
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: bold;">Text Alignment:</label>
                <select id="format-textAlign" style="width: 100%; padding: 5px;">
                    <option value="left" ${currentFormat.textAlign === 'left' ? 'selected' : ''}>Left</option>
                    <option value="center" ${currentFormat.textAlign === 'center' ? 'selected' : ''}>Center</option>
                    <option value="right" ${currentFormat.textAlign === 'right' ? 'selected' : ''}>Right</option>
                </select>
            </div>
            
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: bold;">Font Weight:</label>
                <select id="format-fontWeight" style="width: 100%; padding: 5px;">
                    <option value="normal" ${currentFormat.fontWeight === 'normal' ? 'selected' : ''}>Normal</option>
                    <option value="bold" ${currentFormat.fontWeight === 'bold' ? 'selected' : ''}>Bold</option>
                </select>
            </div>
            
            <div style="display: flex; gap: 10px; justify-content: flex-end; margin-top: 20px;">
                <button id="format-cancel" style="padding: 8px 16px; background: #6c757d; color: white; border: none; border-radius: 4px; cursor: pointer;">Cancel</button>
                <button id="format-apply" style="padding: 8px 16px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;">Apply</button>
            </div>
        `;
        
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);
        
        // Event handlers
        const applyBtn = dialog.querySelector('#format-apply');
        const cancelBtn = dialog.querySelector('#format-cancel');
        
        const closeDialog = () => {
            document.body.removeChild(overlay);
        };
        
        applyBtn.addEventListener('click', () => {
            const fontSize = parseInt(dialog.querySelector('#format-fontSize').value);
            const textColor = dialog.querySelector('#format-textColor').value;
            const backgroundColor = dialog.querySelector('#format-backgroundColor').value;
            const textAlign = dialog.querySelector('#format-textAlign').value;
            const fontWeight = dialog.querySelector('#format-fontWeight').value;
            
            this.updateCellFormat(row, col, {
                fontSize,
                textColor,
                backgroundColor,
                textAlign,
                fontWeight
            });
            
            closeDialog();
        });
        
        cancelBtn.addEventListener('click', closeDialog);
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) closeDialog();
        });
    }
    
    /**
     * Phase 6: Column/Row Resizing
     */
    
    /**
     * Setup column resize functionality
     */
    setupColumnResize(handle, colIndex) {
        let isResizing = false;
        let startX = 0;
        let startWidth = 0;
        
        handle.addEventListener('mousedown', (e) => {
            isResizing = true;
            startX = e.pageX;
            startWidth = this.columnWidths[colIndex];
            
            // Add global listeners
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
            
            // Prevent text selection
            e.preventDefault();
        });
        
        const onMouseMove = (e) => {
            if (!isResizing) return;
            
            const diff = e.pageX - startX;
            const newWidth = Math.max(50, startWidth + diff); // Minimum width 50px
            
            // Update column width
            this.columnWidths[colIndex] = newWidth;
            
            // Update all cells in this column
            this.updateColumnWidth(colIndex, newWidth);
        };
        
        const onMouseUp = () => {
            if (isResizing) {
                isResizing = false;
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }
        };
    }
    
    /**
     * Setup row resize functionality
     */
    setupRowResize(handle, rowIndex) {
        let isResizing = false;
        let startY = 0;
        let startHeight = 0;
        
        handle.addEventListener('mousedown', (e) => {
            isResizing = true;
            startY = e.pageY;
            startHeight = this.rowHeights[rowIndex];
            
            // Add global listeners
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
            
            // Prevent text selection
            e.preventDefault();
        });
        
        const onMouseMove = (e) => {
            if (!isResizing) return;
            
            const diff = e.pageY - startY;
            const newHeight = Math.max(20, startHeight + diff); // Minimum height 20px
            
            // Update row height
            this.rowHeights[rowIndex] = newHeight;
            
            // Update all cells in this row
            this.updateRowHeight(rowIndex, newHeight);
        };
        
        const onMouseUp = () => {
            if (isResizing) {
                isResizing = false;
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            }
        };
    }
    
    /**
     * Update width for all cells in a column
     */
    updateColumnWidth(colIndex, newWidth) {
        // Update data cells in this column (exclude row number cells)
        const cells = this.container.querySelectorAll(`td.data-cell[data-col="${colIndex}"]`);
        cells.forEach(cell => {
            cell.style.width = newWidth + 'px';
        });
        
        // Update header cell - first row in table
        const headerRow = this.table.querySelector('tr');
        if (headerRow) {
            const cells = headerRow.querySelectorAll('td');
            const headerCell = cells[colIndex + 1]; // +1 for corner cell
            if (headerCell && !headerCell.classList.contains('corner-cell')) {
                headerCell.style.width = newWidth + 'px';
            }
        }
    }
    
    /**
     * Update height for all cells in a row
     */
    updateRowHeight(rowIndex, newHeight) {
        // Rows are directly under table, not in tbody (index 0 is header row)
        const rows = this.table.querySelectorAll('tr');
        const targetRow = rows[rowIndex + 1]; // +1 to skip header row
        if (targetRow) {
            const cells = targetRow.querySelectorAll('td');
            cells.forEach(cell => {
                cell.style.height = newHeight + 'px';
            });
        }
    }
    
    /**
     * Phase 4: Basic Formula Engine
     */
    
    /**
     * Evaluate a formula
     */
    evaluateFormula(formula, cellRow, cellCol) {
        try {
            // Remove the leading "="
            const expression = formula.substring(1);
            
            // Parse and evaluate the expression
            return this.evaluateExpression(expression, cellRow, cellCol);
        } catch (error) {
            console.error('Formula error:', error);
            return '#ERROR';
        }
    }
    
    /**
     * Evaluate an expression (formula without the "=")
     */
    evaluateExpression(expression, cellRow, cellCol) {
        // Remove whitespace
        expression = expression.trim();
        
        // Check for functions (SUM, AVERAGE, etc.)
        const functionMatch = expression.match(/^([A-Z]+)\((.+)\)$/i);
        if (functionMatch) {
            const funcName = functionMatch[1].toUpperCase();
            const args = functionMatch[2];
            
            // Parse arguments (could be range or multiple values)
            const rangeMatch = args.match(/^([A-Z]+\d+):([A-Z]+\d+)$/i);
            if (rangeMatch) {
                // It's a range
                const startCell = rangeMatch[1].toUpperCase();
                const endCell = rangeMatch[2].toUpperCase();
                const values = this.getCellRangeValues(startCell, endCell);
                
                // Apply function
                return this.applyFunction(funcName, values);
            } else {
                // Single cell or expression
                return this.applyFunction(funcName, [this.resolveValue(args.trim(), cellRow, cellCol)]);
            }
        }
        
        // Check for cell references (A1, B2, etc.)
        const cellRefMatch = expression.match(/^([A-Z]+)(\d+)$/i);
        if (cellRefMatch) {
            return this.getCellValue(cellRefMatch[1] + cellRefMatch[2]);
        }
        
        // Try to evaluate as a mathematical expression
        try {
            // Replace cell references with their values
            let evaluatedExpression = expression;
            const cellRefs = expression.match(/\b[A-Z]+\d+\b/gi);
            
            if (cellRefs) {
                cellRefs.forEach(cellId => {
                    const value = this.getCellValue(cellId.toUpperCase());
                    evaluatedExpression = evaluatedExpression.replace(new RegExp(cellId, 'gi'), value.toString());
                });
            }
            
            // Use Function constructor for safe evaluation
            // eslint-disable-next-line no-new-func
            return new Function('return ' + evaluatedExpression)();
        } catch (error) {
            console.error('Expression evaluation error:', error);
            return '#ERROR';
        }
    }
    
    /**
     * Apply a function to an array of values
     */
    applyFunction(funcName, values) {
        const nums = values.filter(v => typeof v === 'number' && !isNaN(v));
        
        switch(funcName) {
            case 'SUM':
                return nums.reduce((sum, n) => sum + n, 0);
            case 'AVERAGE':
                return nums.length > 0 ? nums.reduce((sum, n) => sum + n, 0) / nums.length : 0;
            case 'COUNT':
                return nums.length;
            case 'MAX':
                return nums.length > 0 ? Math.max(...nums) : 0;
            case 'MIN':
                return nums.length > 0 ? Math.min(...nums) : 0;
            default:
                return '#FUNC';
        }
    }
    
    /**
     * Resolve a value (could be number or cell reference)
     */
    resolveValue(value, cellRow, cellCol) {
        // Check if it's a number
        const num = parseFloat(value);
        if (!isNaN(num)) {
            return num;
        }
        
        // Check if it's a cell reference
        const cellRefMatch = value.match(/^([A-Z]+)(\d+)$/i);
        if (cellRefMatch) {
            return this.getCellValue(cellRefMatch[1].toUpperCase() + cellRefMatch[2]);
        }
        
        return 0;
    }
    
    /**
     * Get value from a cell
     */
    getCellValue(cellId) {
        // Check if this cell has a calculated value
        if (this.calculatedValues[cellId] !== undefined) {
            return this.calculatedValues[cellId];
        }
        
        // Parse cell ID
        const match = cellId.match(/^([A-Z]+)(\d+)$/);
        if (!match) return 0;
        
        const col = this.columnLetterToIndex(match[1]);
        const row = parseInt(match[2]) - 1; // Convert to 0-based index
        
        // Check bounds
        if (row < 0 || row >= this.numRows || col < 0 || col >= this.numColumns) {
            return 0;
        }
        
        // Get value from data model
        const cellData = this.data[row][col];
        
        // If cell has a formula, evaluate it
        if (cellData.formula) {
            const result = this.evaluateFormula(cellData.formula, row, col);
            this.calculatedValues[cellId] = result;
            return result;
        }
        
        // Return the value (convert to number if possible)
        const numValue = parseFloat(cellData.value);
        return isNaN(numValue) ? 0 : numValue;
    }
    
    /**
     * Get values from a cell range
     */
    getCellRangeValues(startCell, endCell) {
        const startMatch = startCell.match(/^([A-Z]+)(\d+)$/);
        const endMatch = endCell.match(/^([A-Z]+)(\d+)$/);
        
        if (!startMatch || !endMatch) return [];
        
        const startCol = this.columnLetterToIndex(startMatch[1]);
        const startRow = parseInt(startMatch[2]) - 1;
        const endCol = this.columnLetterToIndex(endMatch[1]);
        const endRow = parseInt(endMatch[2]) - 1;
        
        const values = [];
        
        // Iterate through the range
        for (let row = Math.min(startRow, endRow); row <= Math.max(startRow, endRow); row++) {
            for (let col = Math.min(startCol, endCol); col <= Math.max(startCol, endCol); col++) {
                const cellId = this.getCellId(col, row);
                values.push(this.getCellValue(cellId));
            }
        }
        
        return values;
    }
    
    /**
     * Convert column letter(s) to index (A=0, B=1, ..., Z=25, AA=26, etc.)
     */
    columnLetterToIndex(letters) {
        let index = 0;
        for (let i = 0; i < letters.length; i++) {
            index = index * 26 + (letters.charCodeAt(i) - 64);
        }
        return index - 1; // Convert to 0-based
    }
    
    /**
     * Destroy table
     */
    destroy() {
        this.hideContextMenu();
        if (this.container) {
            this.container.remove();
        }
    }
}

// Make globally available
if (typeof window !== 'undefined') {
    window.ExcelTable = ExcelTable;
}

