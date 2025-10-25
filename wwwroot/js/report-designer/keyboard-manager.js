class KeyboardManager {
    constructor(canvasManager) {
        this.canvasManager = canvasManager;
        this.history = [];
        this.historyIndex = -1;
        this.maxHistorySize = 50;
        this.setupKeyboardShortcuts();
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Don't trigger shortcuts when typing in input fields
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.contentEditable === 'true') {
                return;
            }

            const isCtrl = e.ctrlKey || e.metaKey; // Support both Ctrl and Cmd (Mac)
            const isShift = e.shiftKey;
            const key = e.key.toLowerCase();

            // Undo/Redo
            if (isCtrl && key === 'z' && !isShift) {
                e.preventDefault();
                this.undo();
            } else if (isCtrl && (key === 'y' || (key === 'z' && isShift))) {
                e.preventDefault();
                this.redo();
            }

            // Copy/Paste/Delete
            else if (isCtrl && key === 'c') {
                e.preventDefault();
                this.copy();
            } else if (isCtrl && key === 'v') {
                e.preventDefault();
                this.paste();
            } else if (key === 'delete' || key === 'backspace') {
                e.preventDefault();
                this.delete();
            }

            // Select All
            else if (isCtrl && key === 'a') {
                e.preventDefault();
                this.selectAll();
            }

            // Duplicate
            else if (isCtrl && key === 'd') {
                e.preventDefault();
                this.duplicate();
            }

            // Save
            else if (isCtrl && key === 's') {
                e.preventDefault();
                this.save();
            }

            // New Report
            else if (isCtrl && key === 'n') {
                e.preventDefault();
                this.newReport();
            }

            // Preview
            else if (isCtrl && key === 'p') {
                e.preventDefault();
                this.preview();
            }

            // Zoom
            else if (isCtrl && key === '=') {
                e.preventDefault();
                this.canvasManager.zoomIn();
            } else if (isCtrl && key === '-') {
                e.preventDefault();
                this.canvasManager.zoomOut();
            } else if (isCtrl && key === '0') {
                e.preventDefault();
                this.canvasManager.resetZoom();
            }

            // Alignment shortcuts
            else if (isCtrl && key === 'l') {
                e.preventDefault();
                this.canvasManager.alignObjects('left');
            } else if (isCtrl && key === 'e') {
                e.preventDefault();
                this.canvasManager.alignObjects('center');
            } else if (isCtrl && key === 'r') {
                e.preventDefault();
                this.canvasManager.alignObjects('right');
            } else if (isCtrl && key === 't') {
                e.preventDefault();
                this.canvasManager.alignObjects('top');
            } else if (isCtrl && key === 'm') {
                e.preventDefault();
                this.canvasManager.alignObjects('middle');
            } else if (isCtrl && key === 'b') {
                e.preventDefault();
                this.canvasManager.alignObjects('bottom');
            }

            // Group/Ungroup
            else if (isCtrl && key === 'g') {
                e.preventDefault();
                this.canvasManager.groupSelectedObjects();
            } else if (isCtrl && isShift && key === 'g') {
                e.preventDefault();
                this.canvasManager.ungroupSelectedObjects();
            }

            // Layer management
            else if (isCtrl && key === ']') {
                e.preventDefault();
                this.canvasManager.bringToFront();
            } else if (isCtrl && key === '[') {
                e.preventDefault();
                this.canvasManager.sendToBack();
            }

            // Escape key
            else if (key === 'escape') {
                e.preventDefault();
                this.canvasManager.canvas.discardActiveObject();
                this.canvasManager.canvas.renderAll();
            }
        });
    }

    saveState() {
        const canvasState = JSON.stringify(this.canvasManager.canvas.toJSON());
        
        // Remove any states after current index (when user makes new changes after undo)
        this.history = this.history.slice(0, this.historyIndex + 1);
        
        // Add new state
        this.history.push(canvasState);
        this.historyIndex++;
        
        // Limit history size
        if (this.history.length > this.maxHistorySize) {
            this.history.shift();
            this.historyIndex--;
        }
    }

    undo() {
        if (this.historyIndex > 0) {
            this.historyIndex--;
            this.loadState(this.history[this.historyIndex]);
        }
    }

    redo() {
        if (this.historyIndex < this.history.length - 1) {
            this.historyIndex++;
            this.loadState(this.history[this.historyIndex]);
        }
    }

    loadState(state) {
        this.canvasManager.canvas.loadFromJSON(state, () => {
            this.canvasManager.canvas.renderAll();
        });
    }

    copy() {
        const activeObject = this.canvasManager.canvas.getActiveObject();
        if (activeObject) {
            this.canvasManager.canvas.clipboard = activeObject;
        }
    }

    paste() {
        if (this.canvasManager.canvas.clipboard) {
            this.canvasManager.canvas.clipboard.clone((cloned) => {
                this.canvasManager.canvas.discardActiveObject();
                cloned.set({
                    left: cloned.left + 10,
                    top: cloned.top + 10
                });
                this.canvasManager.canvas.add(cloned);
                this.canvasManager.canvas.setActiveObject(cloned);
                this.canvasManager.canvas.renderAll();
                this.saveState();
            });
        }
    }

    delete() {
        const activeObjects = this.canvasManager.canvas.getActiveObjects();
        if (activeObjects.length > 0) {
            activeObjects.forEach(obj => {
                this.canvasManager.canvas.remove(obj);
            });
            this.canvasManager.canvas.discardActiveObject();
            this.canvasManager.canvas.renderAll();
            this.saveState();
        }
    }

    selectAll() {
        this.canvasManager.canvas.discardActiveObject();
        const allObjects = this.canvasManager.canvas.getObjects();
        if (allObjects.length > 0) {
            const selection = new fabric.ActiveSelection(allObjects, {
                canvas: this.canvasManager.canvas
            });
            this.canvasManager.canvas.setActiveObject(selection);
            this.canvasManager.canvas.renderAll();
        }
    }

    duplicate() {
        const activeObject = this.canvasManager.canvas.getActiveObject();
        if (activeObject) {
            activeObject.clone((cloned) => {
                cloned.set({
                    left: cloned.left + 10,
                    top: cloned.top + 10
                });
                this.canvasManager.canvas.add(cloned);
                this.canvasManager.canvas.setActiveObject(cloned);
                this.canvasManager.canvas.renderAll();
                this.saveState();
            });
        }
    }

    save() {
        if (typeof saveCurrentReport === 'function') {
            saveCurrentReport();
        }
    }

    newReport() {
        if (confirm('Are you sure you want to create a new report? Unsaved changes will be lost.')) {
            this.canvasManager.canvas.clear();
            this.history = [];
            this.historyIndex = -1;
        }
    }

    preview() {
        if (typeof previewReport === 'function') {
            previewReport();
        }
    }

    // Initialize with first state
    initialize() {
        this.saveState();
    }
}

// Global instance will be created when canvas is ready
window.KeyboardManager = KeyboardManager;
