
function PatternQueryMetadataEntry(e, vm) {
    "use strict";

    var self = this,
        toggling = false,
        props = mqFilterData.filterMapByProperty[e.Name];

    this.prettyName = props ? props.name : e.Name;
    this.description = props ? props.description : e.Name;

    if (props && props.transform) {
        this.valueFormatter = (function (t) {
            return function (v) {
                return t(mqResultUtils.formatMetadataValue(v));
            };
        })(props.transform);
    } else {
        this.valueFormatter = mqResultUtils.formatMetadataValue;
    }


    this.name = e.Name;
    this.entries = e.Entries;
    this.filteredEntries = ko.observable(this.entries);
    this.filterText = ko.observable("");

    this.isSelected = ko.observable(false);

    this.selectedCount = ko.observable(0);

    this.isSelected.subscribe(function (sel) {
        if (sel) vm.selectedData.push(self);
        else vm.selectedData.remove(self);
    });
    
    this.unselect = function () {
        self.isSelected(false);
    };

    this.toggleSelection = function (into) {
        toggling = true;
        var sel = false, xs = self.filteredEntries();
        _.forEach(xs, function (e) { if (e.isSelected()) { sel = true; return false; } return true; });
        sel = !sel;
        _.forEach(xs, function (e) { e.isSelected(sel); });
        toggling = false;
        self.selectedCount(sel ? xs.length : 0);
    };

    this.setSelection = function (into) {
        toggling = true;
        _.forEach(self.entries, function (e) { e.isSelected(into); });
        toggling = false;
        self.selectedCount(into ? self.entries.length : 0);
    };

    _.forEach(e.Entries, function (x) {
        x.isSelected = ko.observable(false);
        x.isSelected.subscribe(function (sel) {
            if (toggling) return;
            if (sel) self.selectedCount(self.selectedCount() + 1);
            else self.selectedCount(self.selectedCount() - 1);
        });
    });

    function filterEntries(text) {
        var regex = new RegExp(text, "i"), ret = [], sel = 0;

        toggling = true;
        _.forEach(self.entries, function (e) {
            if (regex.test(e.Value)) {
                ret.push(e);
                if (e.isSelected()) sel++;
            }
        });
        toggling = false;
        self.filteredEntries(ret);
        self.selectedCount(sel);
    }

    var filterHandler = mqResultUtils.throttle(filterEntries, 250);
    this.filterText.subscribe(function (text) {
        filterHandler(text);
    });
}

function PatternQueryMetadataViewModel(vm) {
    "use strict";

    var self = this,
        structureSearchInfo = {},
        structureHeatmap = {},        
        patternCount = 0,
        entryCount = 0;

    var updateLabelProxy = _.debounce(function () {
        countFragemntsAndEntries();
        updateLabel();
    }, vm.details.Structures.length < 10000 ? 41 : 100);

    this.data = _.map(vm.details.MetadataSummary, function (e) { var ret = new PatternQueryMetadataEntry(e, self); ret.selectedCount.subscribe(updateLabelProxy); return ret; });
    this.selectedData = ko.observableArray([]);
    this.selectionLabel = ko.observable("Nothing selected");
    this.activeFilterInfo = ko.observable([]);
    this.structureHeatmap = structureHeatmap;

    //var defaultApplyLabel = '<i class="icon-filter icon-white"></i> Apply Filters';
    this.applyLabel = ko.observable('<i class="icon-filter icon-white"></i> Select a category and some values...');
    this.canApply = ko.observable(false);

    this.selectedData.subscribe(updateLabelProxy);

    function updateLabel() {

        var selDataLen = self.selectedData().length,
            zeroCats = [];

        var propCount = 0;

        if (selDataLen === 0) {
            self.selectionLabel("Nothing selected");
        } else {
            _.forEach(self.selectedData(), function (f) {
                var count = f.selectedCount();
                if (count === 0) {
                    zeroCats.push(f.prettyName);
                }
                propCount += count;
            });
            self.selectionLabel(mqResultUtils.pluralize(propCount, "value", "values")
                + " in "
                + mqResultUtils.pluralize(selDataLen, "metadata category", "metadata categories") + " selected"
                + (patternCount === 0
                    ? " [ no matches ]"
                    : " [ " + mqResultUtils.pluralize(patternCount, "pattern", "patterns") + " in " + mqResultUtils.pluralize(entryCount, "PDB entry", "PDB entries") + " ]"));
        }

        if (zeroCats.length > 0) {
            self.applyLabel('<i class="icon-filter icon-white"></i> Select at least one value in <i>' + _.first(zeroCats, 3).join(', ') + '</i>...');
            self.canApply(false);
        } else if (propCount === 0 && selDataLen === 0) {
            self.applyLabel('<i class="icon-filter icon-white"></i> Clear Filter <small>Remove any applied filter</small>');
            self.canApply(true);
        } else {
            self.applyLabel('<i class="icon-filter icon-white"></i> <b>Apply Filter</b> <small>' + self.selectionLabel() + '</small>');
            self.canApply(true);
        }
    }

    function getKey(p, v) {
        if (v === null || v === undefined) return p + ":";
        return p + ":" + v.toString().toLowerCase();
    }

    _.forEach(vm.details.Structures, function (s) {
        var info = { };
        _.forEach(s.Metadata, function (v, p) {
            if (v instanceof Array) {
                if (v.length === 0) info[p] = [getKey(p, "None")];
                else info[p] = _.map(v, function (u) { return getKey(p, u); });
            } else {
                info[p] = [getKey(p, v)];
            }
        });
        structureHeatmap[s.Id] = true;
        structureSearchInfo[s.Id] = info;
    });

    function buildFilterCriteriaAndSetActiveFilter() {
        var filterCriteria = { }, localCriteria, activeFilterInfo = [], localValues;
        _.forEach(self.selectedData(), function (f) {
            localCriteria = { };
            localValues = [];
            _.forEach(f.filteredEntries(), function (e) {
                if (!e.isSelected()) return;

                localCriteria[getKey(f.name, e.Value)] = true;
                localValues.push(e.Value ? e.Value : "NotAssinged");
            });
            activeFilterInfo.push({ prop: f.name, values: localValues });
            filterCriteria[f.name] = localCriteria;
        });        
        self.activeFilterInfo(activeFilterInfo);
        return filterCriteria;
    }

    function updateHeatmapAndActiveFilter() {
        var filters = buildFilterCriteriaAndSetActiveFilter(),
            filterProps = _.keys(filters);
        
        _.forEach(vm.details.Structures, function (s) {
            var include = true,
                info = structureSearchInfo[s.Id];

            _.forEach(filterProps, function (p) {
                var req = filters[p], has = info[p], any = false;
                if (!has) { include = false; return false; }

                _.forEach(has, function (v) {
                    if (req[v]) { any = true; return false; }
                    return true;
                });
                if (!any) { include = false; return false; }
                return true;
            });
            structureHeatmap[s.Id] = include;
        });
    }

    function countFragemntsAndEntries() {
        var filters = buildFilterCriteriaAndSetActiveFilter(),
            filterProps = _.keys(filters),
            eCount = 0, fCount = 0;

        _.forEach(vm.details.Structures, function (s) {
            var include = true,
                info = structureSearchInfo[s.Id];

            _.forEach(filterProps, function (p) {
                var req = filters[p], has = info[p], any = false;
                if (!has) { include = false; return false; }

                _.forEach(has, function (v) {
                    if (req[v]) { any = true; return false; }
                    return true;
                });
                if (!any) { include = false; return false; }
                return true;
            });
            if (include) {
                eCount++;
                fCount += s.PatternCount;
            }
        });

        patternCount = fCount;
        entryCount = eCount;
    }

    this.update = function () {
        updateHeatmapAndActiveFilter();
        vm.motifsGrid.metadataUpdated();
        vm.structuresGrid.metadataUpdated();

        self.applyLabel('<i class="icon-ok icon-white"></i> <b>Filter Applied</b> <small>' + self.selectionLabel() + '</small>');
        self.canApply(false);

        //setTimeout(function () {
        //    $("html").animate({
        //        scrollTop: 450
        //    }, "slow");
        //}, 500);
    };

    this.clear = function () {
        self.selectedData.removeAll();
        _.forEach(self.data, function (f) { f.isSelected(false); f.setSelection(false); });
        setTimeout(function() { self.update() }, 100);
    };

    this.remove = function () {
        self.clear();
        self.update();
    };
}