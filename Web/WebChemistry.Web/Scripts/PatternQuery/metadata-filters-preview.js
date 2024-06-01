function DatabaseFiltersPreviewViewModel(vm) {
    var self = this,
        refreshXhr = null,
        version = 0,
        isPaged = false,
        page = 0;

    this.parent = vm;

    this.fullListHref = ko.observable("#");
    this.fullMetadataHref = ko.observable("#");

    var refreshMessage = "← Click to view data.";

    this.isRefreshing = ko.observable(false);
    this.canPage = ko.observable(false);
    this.structureCountString = ko.observable(refreshMessage);


    this.pageNumber = ko.observable("-");
    this.pageCount = ko.observable("of n/a");

    this.currentPreview = [];

    this.previewChanged = ko.observable();

    this.state = ko.observable({
        hasError: false,
        hasData: false
    });
        
    vm.changed.subscribe(function () {
        var action = PatternQueryActions.databaseAction,
            filters = "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters())));
        self.fullListHref(action.replace("-type-", "List") + filters);
        self.fullMetadataHref(action.replace("-type-", "Metadata") + filters);
    });

    function sortData(column) {
        var data = self.state().data, sortFunc;
        if (!data || !column) return;

        if (column.asc) {
            sortFunc = (function (getter) {
                return function (a, b) {
                    var x = getter(a), y = getter(b), u = +x, v = +y;
                    if (!isNaN(u) && !isNaN(v)) { x = u; y = v; }
                    return (x === y ? 0 : (x > y ? 1 : -1));
                };
            })(column.getter);
        } else {
            sortFunc = (function (getter) {
                return function (a, b) {
                    var x = getter(a), y = getter(b), u = +x, v = +y;
                    if (!isNaN(u) && !isNaN(v)) { x = u; y = v; }
                    return (x === y ? 0 : (x > y ? -1 : 1));
                };
            })(column.getter);
        }

        column.asc = !column.asc;

        data.sort(sortFunc)
    }
    
    function update(preview, isRandom) {
        var size = (preview['resultSize'] || 0),
            infoStr, pageStart, pageCount, pageSize = 15;

        self.hasError = false;
        self.previewData = JSON.stringify(preview);

        if (isPaged) {
            pageCount = (size / pageSize) | 0;
            if (size % pageSize > 0) pageCount++;
            pageStart = page % pageCount;
            if (pageStart < 0) pageStart += pageCount;
            page = pageStart;
            if (size === 0) pageStart = -1;
            infoStr = "" + size + " matching PDB entries." + (size > 0 ? " Showing page " + (pageStart + 1) + " of " + pageCount + " (entries " + (pageStart * pageSize + 1) + "–" + Math.min(pageStart * pageSize + pageSize, size) + ")." : "");
            self.pageNumber((pageStart + 1).toString());
            self.pageCount("of " + pageCount);
        } else {
            infoStr = "" + size + " matching PDB entries." + (size > 0 ? " Showing " + (isRandom ? "random " : "first ") + Math.min(preview.preview.length, size) + "." : "");
            if (size > 0) {
                self.pageNumber("1");
                self.pageCount("of 1");
            } else {
                self.pageNumber("0");
                self.pageCount("of 0");
            }
        }

        self.structureCountString(infoStr);
        preview.preview = preview.preview || [];
        if (preview.preview.length === 0) {
            self.state({});
            self.currentPreview = [];
            self.previewChanged(++version);
            return;
        }

        var first = preview.preview[0],
            cols = [];

        var excludeColumns = { "SizeInBytes": true /*, "HostOrganismsId": true, "OriginOrganismsId": true */ };

        var sortedProps = _.sortBy(_.keys(first.props), function (p) {
            var m = mqFilterData.filterMapByProperty[p];
            if (m) return m.priority;
            return 1000;
        });
        
        _.forEach(mqFilterData.defaultColumns, function (col) {
            var p = col.property, info = col, render;

            if (excludeColumns[p]) return;

            //var info = //mqFilterData.filterMapByProperty[p];

            var getter = (function (_p, t) {
                return function (e) {
                    var v = e.props[_p];
                    if (v === 0) v = "0";
                    return t(v || "").replace(/¦/g, ' | ');
                }
            })(p, info.transform);

            
            if (info.type === mqFilterData.filterTypeRing) {
                var render = (function (_p) {
                    return function (e) {
                        var v = e.props[_p];
                        if (!v) return "<span style='color:#BBB'>-</span>";
                        
                        return mqFilterData.ringsRender(v, "<span style='color:#BBB'> | </span>");
                    }
                })(p);
            } else {
                var render = (function (_p, t) {
                    return function (e) {
                        var v = e.props[_p];
                        if (v === 0) v = "0";
                        if (!v) return "<span style='color:#BBB'>-</span>";
                        return t(v || "").replace(/[¦<>&]/g, function (c) {
                            if (c === '<') return '&lt;';
                            if (c === '>') return '&gt;';
                            if (c === '&') return '&amp;';
                            return "<span style='color:#BBB'> | </span>";
                        });
                    }
                })(p, info.transform);
            }

            cols.push({
                asc: true,
                header: info.name,
                tooltip: info.name + ": " + info.description,
                getter: getter,
                render: render,
                numeric: info.type === mqFilterData.filterTypeNumeric
            });
        });

        _.forEach(preview.preview, function (e) {
            e.cols = cols;
            if (window['PatternQueryMetadataPreview'] && PatternQueryMetadataPreview.includePreviewSelection) {
                e.sel = ko.observable(false);
            }
        });

        var idCol = {
            asc: true,
            header: "Id",
            tooltip: "Id: PDB Identifier",
            getter: function (e) { return e.id; },
            render: function (e) { return e.id; },
            numeric: false
        };

        self.state({
            idColumn: idCol,
            sortData: sortData,
            hasData: true,
            cols: cols,
            data: new ko.observableArray(preview.preview)
        });

        self.currentPreview = preview.preview;
        self.previewChanged(++version);
    }

    function handleErrors(errors) {
        self.structureCountString("Error.");
        self.state({
            hasError: true,
            errors: errors
        });
        self.currentPreview = [];
        self.previewChanged(++version);
    }

    this.clear = function () {
        try {
            if (refreshXhr != null) {
                refreshXhr.abort();
                refreshXhr = null;
            }
        } catch (e) { }

        self.state({
            hasError: false,
            hasData: false
        });
        self.isRefreshing(false);
        self.structureCountString(refreshMessage);
        self.pageNumber("-");
        self.pageCount("of n/a");
        self.currentPreview = [];
        self.previewChanged(++version);
    };

    function refresh(action, isRandom) {
        self.isRefreshing(true);
        refreshXhr = $.ajax(action, {
            dataType: 'json',
            success: function (data) {
                if (data['state'] === 'ok') {
                    update(data, isRandom);
                } else if (data['state'] === 'error') {
                    handleErrors(data['errors'] || ['Ooops, something went terribly wrong. Please try again later.']);
                } else {
                    handleErrors(['Ooops, something went terribly wrong. Please try again later.']);
                }
            },
            error: function (x, y, z) {
                handleErrors(['Ooops, something went terribly wrong. Please try again later.']);
                console.log(z);
            },
            complete: function () { refreshXhr = null; self.isRefreshing(false); self.canPage(isPaged); }
        });
    }

    this.refresh = function () {
        refresh(PatternQueryActions.databaseAction.replace("-type-", "Preview") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))));
    };

    this.refreshTop15 = function () {
        self.canPage(false);
        isPaged = true;
        page = 0;
        refresh(PatternQueryActions.databaseAction.replace("-type-", "Paged15") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))) + "&page=0");
    };

    this.prevPage = function () {
        self.canPage(false);
        page--;
        refresh(PatternQueryActions.databaseAction.replace("-type-", "Paged15") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))) + "&page=" + encodeURIComponent(page));
    };

    this.nextPage = function () {
        self.canPage(false);
        page++;
        refresh(PatternQueryActions.databaseAction.replace("-type-", "Paged15") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))) + "&page=" + encodeURIComponent(page));
    };

    this.refreshRandom15 = function () {
        self.canPage(false);
        isPaged = false;
        refresh(PatternQueryActions.databaseAction.replace("-type-", "PreviewRandom15") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))), true);
    };

    this.gotoPage = function () {
        self.canPage(false);
        isPaged = true;
        page = +self.pageNumber();
        if (isNaN(page)) page = 0;
        else page = page - 1;
        refresh(PatternQueryActions.databaseAction.replace("-type-", "Paged15") + "?filters=" + encodeURIComponent(btoa(JSON.stringify(vm.getFilters()))) + "&page=" + encodeURIComponent(page));
    };
}