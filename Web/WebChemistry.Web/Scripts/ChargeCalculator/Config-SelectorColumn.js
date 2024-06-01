(function ($) {
    // register namespace
    $.extend(true, window, {
        "Slick": {
            "ChargesCheckboxSelectColumn": ChargesCheckboxSelectColumn
        }
    });


    function ChargesCheckboxSelectColumn(dataView, options) {
        var _grid;
        var _self = this;
        var _handler = new Slick.EventHandler();
        var _selectedRowsLookup = {};
        var _defaults = {
            columnId: "_checkbox_selector",
            cssClass: null,
            toolTip: "Select/Deselect All",
            width: 30
        };

        var _options = $.extend(true, {}, _defaults, options);

        dataView.onRowCountChanged.subscribe(function (e, args) {
            handleSelectedRowsChanged();
        });

        this.update = handleSelectedRowsChanged;

        function init(grid) {
            _grid = grid;
            _handler
              .subscribe(_grid.onSelectedRowsChanged, handleSelectedRowsChanged)
              .subscribe(_grid.onClick, handleClick)
              .subscribe(_grid.onHeaderClick, handleHeaderClick)
              .subscribe(_grid.onKeyDown, handleKeyDown);            
        }

        function destroy() {
            _handler.unsubscribeAll();
        }

        function handleSelectedRowsChanged(e, args) {
            
            for (i in _selectedRowsLookup) {
                _grid.invalidateRow(i);
            };

            var selectedRows = [], row, i;
            var lookup = {};
            var row;
            for (i = 0; i < _grid.getDataLength() ; i++) {
                row = _grid.getDataItem(i);
                if (row["sel"] && row["sel"]()) {
                    selectedRows.push(i);
                    lookup[i] = true;
                }
            }

            var selectedRowsGrid = _grid.getSelectedRows();
            if (selectedRowsGrid.length !== selectedRows.length) {
                _grid.setSelectedRows(selectedRows);
            } else {
                for (i = 0; i < selectedRowsGrid.length; i++) {
                    if (!lookup[selectedRowsGrid[i]]) {
                        _grid.setSelectedRows(selectedRows);
                        break;
                    }
                }
            }
            
            _grid.invalidateRows(selectedRows);
            _selectedRowsLookup = lookup;
            _grid.render();
        }

        function handleKeyDown(e, args) {
            if (e.which === 32) {
                if (_grid.getColumns()[args.cell].id === _options.columnId) {
                    // if editing, try to commit
                    if (!_grid.getEditorLock().isActive() || _grid.getEditorLock().commitCurrentEdit()) {
                        toggleRowSelection(args.row);
                    }
                    e.preventDefault();
                    e.stopImmediatePropagation();
                }
            }
        }

        function handleClick(e, args) {
            // clicking on a row select checkbox
            if (_grid.getColumns()[args.cell].id === _options.columnId && $(e.target).is(":checkbox")) {
                // if editing, try to commit
                if (_grid.getEditorLock().isActive() && !_grid.getEditorLock().commitCurrentEdit()) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    return;
                }

                toggleRowSelection(args.row);
                e.stopPropagation();
                e.stopImmediatePropagation();
            }
        }

        function toggleRowSelection(row) {
            if (_selectedRowsLookup[row]) {
                _grid.getDataItem(row)["sel"](false);
                _grid.setSelectedRows($.grep(_grid.getSelectedRows(), function (n) {
                    return n !== row
                }));
            } else {
                _grid.getDataItem(row)["sel"](true);
                _grid.setSelectedRows(_grid.getSelectedRows().concat(row));
            }
        }

        function handleHeaderClick(e, args) {
            var $target = $(e.target);
            if (args.column.id === _options.columnId && $target.is(":checkbox")) {
                // if editing, try to commit
                if (_grid.getEditorLock().isActive() && !_grid.getEditorLock().commitCurrentEdit()) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    return;
                }

                if ($target.is(":checked")) {
                    var rows = [];
                    for (var i = 0; i < _grid.getDataLength() ; i++) {
                        rows.push(i);
                    }
                    _grid.setSelectedRows(rows);
                } else {
                    _grid.setSelectedRows([]);
                }
                e.stopPropagation();
                e.stopImmediatePropagation();
            }
        }

        function getColumnDefinition() {
            return {
                id: _options.columnId,
                name: "", //"<input type='checkbox'>",
                toolTip: _options.toolTip,
                field: "sel",
                width: _options.width,
                resizable: false,
                sortable: false,
                cssClass: _options.cssClass,
                formatter: checkboxSelectionFormatter
            };
        }

        function checkboxSelectionFormatter(row, cell, value, columnDef, dataContext) {
            if (dataContext) {
                return dataContext["sel"]()
                    ? "<input data-row='" + row + "' type='checkbox' checked='checked'>"
                    : "<input data-row='" + row + "' type='checkbox'>";
            }
            return null;
        }

        $.extend(this, {
            "init": init,
            "destroy": destroy,

            "getColumnDefinition": getColumnDefinition
        });
    }
})(jQuery);