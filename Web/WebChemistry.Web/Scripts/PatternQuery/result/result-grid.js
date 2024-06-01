function setCurrentMotif(event, q, m) {
    if (event) event.preventDefault();
    var d = PatternQueryResultModel.detailsMap[q], n = d ? d.motifMap[m] : null;
    if (n) d.currentMotif(n);
}

function setCurrentStructure(event, q, s) {
    if (event) event.preventDefault();
    var d = PatternQueryResultModel.detailsMap[q], n = d ? d.structureMap[s] : null;
    if (n) d.currentStructure(n);
}

function showCurrentStructure(event, q, s) {
    if (event) event.preventDefault();
    var d = PatternQueryResultModel.detailsMap[q], n = d ? d.structureMap[s] : null;
    if (n) {
        $('#' + q + '-details-data a[data-details-by=structure]').click();
        d.currentStructure(n);
    }
}

function showMotifsByStructure(event, q, s) {
    if (event) event.preventDefault();
    var d = PatternQueryResultModel.detailsMap[q], n = d ? d.structureMap[s] : null;
    if (n) {
        $('#' + q + '-details-data a[data-details-by=motif]').click();
        d.motifsFilterText('^' + s + '_');
    }
}

function PatternQueryGridViewModel(isStructures, vm) {
    "use strict";

    var self = this,
        initialized = false;

    var defaultColumns;
    
    var fieldCache = {};
    var renderStructureIdCell = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache[k];

            if (val === undefined) {
                var p = vm.structureMap[value], cls;
                if (p.ErrorType === "None") {
                    cls = p.ComputationWarnings.length ? 'mq-computation-warning' : (p.ReaderWarnings.length > 0 ? 'mq-reader-warning' : 'mq-structure-valid');
                } else {
                    cls = 'mq-structure-error';
                }
                //val = "<a class='" + cls + "' href='#' onclick='javascript:setCurrentStructure(event, \"" + vm.id + "\", \"" + value + "\")'>" + value + "</a>";
                val = "<a class='" + cls + "' href='#' data-show-type='current-structure' data-id='" + value + "'>" + value + "</a>";
                fieldCache[k] = val;
            }
            return val;
        };
    }(vm, fieldCache);

    var renderMotifIdCell = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache[k];

            if (val === undefined) {
                var cls = dataContext.ValidationFlagsId >= 0 
                    ? vm.validations[dataContext.ValidationFlagsId].css 
                    : "mq-motif-ok";

                //val = "<a href='#' class='" + cls + "' onclick='javascript:setCurrentMotif(event, \"" + vm.id + "\", \"" + value + "\")'>" + value + "</a>";
                val = "<a href='#' class='" + cls + "' data-show-type='current-motif' data-id='" + value + "'>" + value + "</a>";
                fieldCache[k] = val;
            }

            return val;
        };
    }(vm, fieldCache);

    var renderMotifCountCell = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache[k];

            if (val === undefined) {
                val = "<a href='#' style='color: blue' data-show-type='show-motifs' data-id='" + dataContext.Id + "'>" + value + "</a>";
                fieldCache[k] = val;
            }
            return val;
        };
    }(vm, fieldCache);

    var renderParentIdCell = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, val = fieldCache[k];

            if (val === undefined) {
                var p = vm.structureMap[value], cls;
                if (p.ErrorType === "None") {
                    cls = p.ComputationWarnings.length ? 'mq-computation-warning' : (p.ReaderWarnings.length > 0 ? 'mq-reader-warning' : 'mq-structure-valid');
                } else {
                    cls = 'mq-structure-error';
                }
                val = "<a class='" + cls + "' href='#' data-show-type='show-structure' data-id='" + value + "'>" + value + "</a>";
                fieldCache[k] = val;
            }
            return val;
        };
    }(vm, fieldCache);

    var signatureCellFormatter = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.id + "-" + dataContext.Id, sig = fieldCache[k];
            if (sig === undefined) {
                sig = "<span title='" + value + "'>" + mqResultUtils.formatSignature(value) + "</span>";
                fieldCache[k] = sig;
            }
            return sig;
        };
    }(vm, fieldCache);

    var metadataCellFormatter = function (vm, fieldCache) {
        return function (row, cell, value, columnDef, dataContext) {
            var k = columnDef.metaField + "-" + value, v = fieldCache[k], props;
            if (v === undefined) {
                props = mqFilterData.filterMapByProperty[columnDef.metaField];
                if (props && props.transform) {
                    v = props.transform(mqResultUtils.formatMetadataValue(vm.structureMap[value].Metadata[columnDef.metaField]));
                } else {
                    v = mqResultUtils.formatMetadataValue(vm.structureMap[value].Metadata[columnDef.metaField]);
                }
                v = "<span title='" + v + "'>" + v + "</span>";
                fieldCache[k] = v;
            }
            return v;
        };
    }(vm, fieldCache);

    if (isStructures) {
        defaultColumns = [
            { id: "id", field: "Id", name: "Id", sortable: true, width: 100, toolTip: "PDB ID.", formatter: renderStructureIdCell, resizable: false },
            { id: "mc", field: "PatternCount", name: "Patterns", sortable: true, width: 60, toolTip: "Number of patterns in the structure.", cssClass: 'mq-numeric-cell', formatter: renderMotifCountCell, resizable: false, numeric: true },
            { id: "ac", field: "AtomCount", name: "Atoms", sortable: true, width: 60, toolTip: "Number of atoms in the structure.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rc", field: "ResidueCount", name: "Residues", sortable: true, width: 60, toolTip: "Number of residues in the structure.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "wc", field: "warningCount", name: "Warnings", sortable: true, width: 60, toolTip: "Number of warnings.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true }//,
            //{ id: "lt", field: "LoadTimingMs", name: "Load Time", sortable: true, width: 60, toolTip: "Time it took to load the structure (in ms).", cssClass: 'mq-numeric-cell', resizable: false },
            //{ id: "qt", field: "QueryTimingMs", name: "Query Time", sortable: true, width: 60, toolTip: "Time it took to query the structure (in ms).", cssClass: 'mq-numeric-cell', resizable: false }
        ];
    } else {
        defaultColumns = [
            { id: "id", field: "Id", name: "Id", sortable: true, width: 100, toolTip: "Pattern identifier (parent_serial[_tag]).", formatter: renderMotifIdCell, resizable: false },
            { id: "pid", field: "ParentId", name: "Parent", sortable: true, width: 60, toolTip: "Parent PDB entry identifier.", formatter: renderParentIdCell, resizable: false },
            { id: "ac", field: "AtomCount", name: "Atoms", sortable: true, width: 60, toolTip: "Number of atoms in the pattern.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rc", field: "ResidueCount", name: "Residues", sortable: true, width: 60, toolTip: "Number of residues in the pattern.", cssClass: 'mq-numeric-cell', resizable: false, numeric: true },
            { id: "rs", field: "Signature", name: "Signature", sortable: true, width: 160, toolTip: "Residue types in the pattern.", formatter: signatureCellFormatter, resizable: false }
        ];
    }
    //var fillerColumn = { id: "filler", field: "", name: "", sortable: false };

    var gridOptions = {
        enableCellNavigation: false,
        enableColumnReorder: false,
        multiSelect: false,
        explicitInitialization: true,
        forceFitColumns: true,
        editable: false
    };

    var dataView = new Slick.Data.DataView();
    dataView.setItems(isStructures ? vm.details.Structures : vm.details.Patterns, "Id");

    var grid = new Slick.Grid("#" + vm.id + (isStructures ? "-structures-grid" : "-motifs-grid"), dataView, [], gridOptions);
    dataView.onRowCountChanged.subscribe(function (e, args) { grid.updateRowCount(); grid.render(); });
    dataView.onRowsChanged.subscribe(function (e, args) { grid.invalidateRows(args.rows); grid.render(); });
    //grid.setColumns(defaultColumns.concat([fillerColumn]));
    
    grid.onSort.subscribe(function (e, args) {
        var sortCol = args.sortCol,
            sortFunc;

        if (/^meta-/.test(sortCol.id)) {
            if (isStructures) {
                sortFunc = (function (prop) {
                    return function (a, b) {
                        var x = a.Metadata[prop], y = b.Metadata[prop];
                        return (x === y ? 0 : (x > y ? 1 : -1));
                    };
                })(sortCol.metaField);
            } else {
                sortFunc = (function (prop, map) {
                    return function (a, b) {
                        var x = map[a.ParentId].Metadata[prop], y = map[b.ParentId].Metadata[prop];
                        return (x === y ? 0 : (x > y ? 1 : -1));
                    };
                })(sortCol.metaField, vm.structureMap);
            }
        } else {
            if (isStructures || sortCol.id !== 'id') {
                sortFunc = (function (prop) {
                    return function (a, b) {
                        var x = a[prop], y = b[prop];
                        return (x === y ? 0 : (x > y ? 1 : -1));
                    };
                })(sortCol.field);
            } else {
                sortFunc = function (a, b) {
                    var x = a.ParentId, y = b.ParentId;
                    if (x === y) {
                        var u = a.Serial, v = b.Serial;
                        return (u === v ? 0 : (u > v ? 1 : -1));
                    }
                    return (x === y ? 0 : (x > y ? 1 : -1));
                };
            }
        }
        dataView.sort(sortFunc, args.sortAsc);
    });

    this.filterText = ko.observable("");

    var updateFilterFromText = mqResultUtils.throttle(updateFilter, 250);
    this.filterText.subscribe(updateFilterFromText);

    this.filterType = ko.observable("All");  // All/ReaderWarning/ComputationWarning/Error
    this.filterType.subscribe(updateFilterFromText);

    function updateFilter() {
        if (isStructures && vm.currentBy !== 'structure') return;
        if (!isStructures && vm.currentBy !== 'motif') return;

        var heatmap = vm.metadata.structureHeatmap,
            regexp = new RegExp(self.filterText().trim(), 'i'),
            filter, i;

        if (isStructures) {
            switch (self.filterType()) {
                case "ReaderWarning": filter = (function (map, re) { return function (e) { return e.ReaderWarnings.length > 0 && map[e.Id] && re.test(e.Id); }; })(heatmap, regexp); break;
                case "ComputationWarning": filter = (function (map, re) { return function (e) { return e.ComputationWarnings.length > 0 && map[e.Id] && re.test(e.Id); }; })(heatmap, regexp); break;
                case "Error": filter = (function (map, re) { return function (e) { return e.ErrorType !== "None" && map[e.Id] && re.test(e.Id); }; })(heatmap, regexp); break;
                case "WithMotif": filter = (function (map, re) { return function (e) { return e.PatternCount > 0 && map[e.Id] && re.test(e.Id); }; })(heatmap, regexp); break;
                default: filter = (function (map, re) { return function (e) { return map[e.Id] && re.test(e.Id); }; })(heatmap, regexp); break;
            }
        } else {
            switch (self.filterType()) {
                case "None": filter = (function (map, re, vals) { return function (e) { return (e.ValidationFlagsId < 0 || vals[e.ValidationFlagsId].problem === "None") && (map[e.ParentId] && re.test(e.Id)); }; })(heatmap, regexp, vm.validations); break;
                case "ChiralityMinor": filter = (function (map, re, vals) { return function (e) { return (e.ValidationFlagsId >= 0 && vals[e.ValidationFlagsId].problem === "ChiralityMinor") && (map[e.ParentId] && re.test(e.Id)); }; })(heatmap, regexp, vm.validations); break;
                case "Chirality": filter = (function (map, re, vals) { return function (e) { return (e.ValidationFlagsId >= 0 && vals[e.ValidationFlagsId].problem === "Chirality") && (map[e.ParentId] && re.test(e.Id)); }; })(heatmap, regexp, vm.validations); break;
                case "Incomplete": filter = (function (map, re, vals) { return function (e) { return (e.ValidationFlagsId >= 0 && vals[e.ValidationFlagsId].problem === "Incomplete") && (map[e.ParentId] && re.test(e.Id)); }; })(heatmap, regexp, vm.validations); break;
                default: filter = (function (map, re) { return function (e) { return map[e.ParentId] && re.test(e.Id); }; })(heatmap, regexp); break;
            }
        }

        dataView.setFilter(filter);

        var structures = { }, len = dataView.getLength(), structureCount, motifCount, tt;
        if (isStructures) {
            structureCount = len;
            motifCount = 0;
            for (i = 0; i < len; i++) {
                motifCount += dataView.getItem(i).PatternCount || 0;
                structures[tt] = true;
            }
            vm.structuresViewCount({ motifs: motifCount, structures: structureCount });
        } else {
            motifCount = len;
            structureCount = 0;
            for (i = 0; i < len; i++) {
                tt = dataView.getItem(i).ParentId;
                if (!structures[tt]) {
                    structureCount++;
                    structures[tt] = true;
                }
            }
            vm.motifsViewCount({ motifs: motifCount, structures: structureCount });
        }
    }
            
    this.metadataUpdated = function () {
        if (isStructures && vm.currentBy !== 'structure') return;
        if (!isStructures && vm.currentBy !== 'motif') return;

        var totalW = 910, width = 0, xs = vm.metadata.selectedData(), cw, columns, last = defaultColumns[defaultColumns.length - 1];

        if (!isStructures) last.width = 160;

        _.forEach(defaultColumns, function (c) { width += c.width || 0; });

        if (xs.length > 0) {
            cw = (totalW - width) / xs.length;
            columns = defaultColumns.concat(
                _.map(xs, function (f) {
                    return {
                        id: "meta-" + f.name, field: isStructures ? "Id" : "ParentId", metaField: f.name,
                        name: mqFilterData.filterMapByProperty[f.name] ? mqFilterData.filterMapByProperty[f.name].name : f.name,
                        sortable: true, width: cw,
                        toolTip: mqFilterData.filterMapByProperty[f.name] ? mqFilterData.filterMapByProperty[f.name].description : f.name,
                        formatter: metadataCellFormatter, resizable: false
                    };
                }));
        } else {
            if (isStructures) {
                columns = defaultColumns.concat([{ id: "filler", field: "", name: "", width: totalW - width, resizable: false, sortable: false }]);
            } else {
                last = defaultColumns[defaultColumns.length - 1];
                cw = (totalW - width + last.width);
                last.width = cw;
                columns = defaultColumns;
            }
        }

        grid.setColumns(columns);
        updateFilter();
    };

    this.toCsv = function () {
        var cols = _.map(_.filter(grid.getColumns(), function (c) { return c.id !== 'filler'; }), function (c) {

            if (c.metaField) {
                var numeric = mqFilterData.isNumeric(c.metaField);
                if (isStructures) {
                    return { header: c.name, isNumeric: numeric, getter: function (r) { return r.Metadata[c.metaField]; } };
                } else {
                    return {
                        header: c.name,
                        isNumeric: numeric,
                        getter: function (c, map) {
                            return function (r) { return mqResultUtils.formatMetadataValue(map[r.ParentId].Metadata[c.metaField]); };
                        }(c, vm.structureMap)
                    };
                }
            } else {
                return { header: c.name, isNumeric: c.numeric, getter: function (r) { return r[c.field]; } };
            }
        });

        return WebChemUtils.toCsv(dataView.getRows(), cols);
    };

    this.init = function () {
        if (!initialized) {
            grid.init();
            updateFilter();
            self.metadataUpdated();
            initialized = true;
        } else {
            updateFilter();
            self.metadataUpdated();
        }
    };
}