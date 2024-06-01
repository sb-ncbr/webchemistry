function ResultOverviewViewModel(result, element) {
    var summary = {}, structures = {};

    _.forEach(result.Models, function (m) {
        var ms = m.Summary;
        $.each(ms, function (key, value) {
            summary[key] = (summary[key] || 0) + value;            
        });

        $.each(m.StructureNames, function (i, name) {
            structures[name.toLowerCase()] = true;
        });
    });

    var initialized = false;

    this.motifCount = summary["Analyzed"] || 0;
    if (this.motifCount === 1) {
        this.motifCountText = this.motifCount.toString() + " molecule";
    } else {
        this.motifCountText = this.motifCount.toString() + " molecules";
    }
    this.structureCount = Object.keys(structures).length;
    if (this.structureCount === 1) {
        this.structureCountText = this.structureCount.toString() + " PDB entry";
    } else {
        this.structureCountText = this.structureCount.toString() + " PDB entries";
    }
    this.modelCount = result.Models.length;
    if (this.modelCount === 1) {
        this.modelCountText = this.modelCount.toString() + " model";
    } else {
        this.modelCountText = this.modelCount.toString() + " models";
    }

    this.init = function () {
        if (initialized) {
            return;
        }
        initialized = true;
        makeOverviewPlot(summary, element);
    };
}