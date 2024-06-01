
var mqResultUtils = {
    formatMetadataValue: function (v) {
        if (v instanceof Array) return v.length === 0 ? "None Specified" : v.join('; ');
        if (v === "None") return "None Specified";
        if (v === "NotAssigned") return "Not Assigned";
        return v === null ? "Not Assigned" : v;
    },
    formatSignature: function (s) {
        var re = /([^\*-]+)\*([0-9]+)/g;
        var m, ret = "";
        while ((m = re.exec(s)) != null) {
            if (m.index === re.lastIndex) {
                re.lastIndex++;
            }

            ret += m[1] + "<sub>" + m[2] + "</sub>";
        }
        return ret;
    },
    formatMsTime: function (totalTimeMs) {
        if (totalTimeMs > 60 * 60 * 1000) {
            return Math.floor(totalTimeMs / (1000 * 60 * 60)).toString() + "hr " + Math.floor((totalTimeMs % (1000 * 60 * 60)) / (1000 * 60)).toString() + "min " + Math.floor((totalTimeMs % (1000 * 60)) / (1000)).toString() + "sec";
        } else if (totalTimeMs > 60 * 1000) {
            return Math.floor(totalTimeMs / (1000 * 60)).toString() + "min " + Math.floor((totalTimeMs % (1000 * 60)) / 1000).toString() + "sec";
        } else {
            return Math.floor(totalTimeMs / 1000).toString() + "sec";
        }
    },
    throttle: function (f, delay) {
        var timeout = null;
        var ret = function (arg) {
            if (timeout !== null) {
                clearTimeout(timeout);
            }
            timeout = setTimeout((function (arg) { return function () { timeout = null; f.call(null, arg); }; })(arg), delay);
        };
        return ret;
    },
    pluralize: function (count, sing, plural) {
        if (count === 1) return "1 " + sing;
        return count.toString() + " " + plural;
    },
    isVersionOk: function (version, minSvcVersion) {
        var regex = /(\d+)\.(\d+)\.(\d+)\.(\d+)\.(\d+)(.*)/;

        var fields = regex.exec(version);

        if (!fields) {
            return false;
        }

        var minFields = regex.exec(minSvcVersion),
            minVersion = [+minFields[1], +minFields[2], +minFields[3], +minFields[4], +minFields[5], minFields[6]],
            currentVersion = [+fields[1], +fields[2], +fields[3], +fields[4], +fields[5], fields[6]];
        return minVersion <= currentVersion;
    },
    formatResidueValidations: function (motif, detailsVm) {
        var rVals = detailsVm.structureMap[motif.ParentId].ResidueValidationIds,
            vals = detailsVm.validations;
        var rs = _.map(motif.Residues.split("-"), function (r) {
            var v = rVals[r], t;
            if (v < 0 || !(t = vals[v])) return "<span class='mq-motif-notvalidated-residue'>" + r + "</span>";
            var resId = r.substr(r.indexOf(' ') + 1).replace(/\si:.*/, '');
            return "<a class='mq-molecule-validation-link " + t.css + "' title='" + t.text + "' target='_blank' " +
                "href='" + PatternQueryActions.moleculeValidationProvider(motif.ParentId, resId) + "'>"
                + r + "</a>";
        });
        return rs.join(" - ");
    },    
    jsonToChemDoodleMolecule: function (data) {
        var mol = new ChemDoodle.structures.Molecule();
        var atoms = _.mapValues(data.Atoms, function (a) {
            var atom = new ChemDoodle.structures.Atom(a.Symbol, a.Position[0], a.Position[1], a.Position[2]);
            atom.desc = a.Symbol + " " + a.Id;
            return atom;
        });
        _.forEach(atoms, function (a) { mol.atoms.push(a); });
        var bonds = _.forEach(data.Bonds, function (b) {
            var order = 1;
            //if (b.Type === "Double") order = 2;
            //else if (b.Type === "Triple") order = 3;
            mol.bonds.push(new ChemDoodle.structures.Bond(atoms[b.A], atoms[b.B], order));
        });

        if (data.Residues) {
            _.forEach(data.Residues, function (r) {
                _.forEach(r.Atoms, function (a) {
                    var atom = data.Atoms[a];
                    atoms[a].desc = (atom.Name || atom.Symbol) + " " + (atom.SerialNumber || atom.Id) + " (" + r.Name + " " + r.SerialNumber + (r.Chain.trim().length > 0 ? " " + r.Chain : "") + ")";
                });
            });
        }
        return mol;
    },
    prettifyQuery: function (query) {
        query = "" + query;

        if (query.length < 3) return query;
        
        if (query[0] === '(' && query[query.length - 1] === ')') {
            return query.substring(1, query.length - 1);
        }

        return query;
    }
};