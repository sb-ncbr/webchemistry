function init3d(result) {
    "use strict";

    function Visualizer(result, target) {
        var self = this;

        var displayModes = {
            "BallsAndSticks": {
                type: "BallsAndSticks",
                name: "Balls and Sticks",
                description: "Render atoms as spheres and bonds as tubes."
            },
            "Surface": {
                type: "Surface",
                name: "Surface",
                description: "Render the surface of the molecule.<small><br/>" +
                    "Each atom has a <abbr title='At point X given by (VDW Radius + 2 * Probe Radius) / |Atom Position - X| - 1.'>potential</abbr> " +
                    "that determines the <abbr title='Points with the sum of potential over all atoms equal to 1.'>surface</abbr>.</small>"
            },
            "VDWSpheres": {
                type: "VDWSpheres",
                name: "VDW Spheres",
                description: "Render atoms as spheres with VDW radius."
            },
            "Cartoons": {
                type: "Cartoons",
                name: "Cartoons",
                description: "Render amino acids and nucleotides as cartoons.<small><br/>HET atoms are optionally rendered as Balls and Sticks. " +
                    "The secondary structure is <abbr title='Using method from Zhang, Y., &amp; Skolnick, J. (2005). TM-align: a protein structure alignment algorithm based on the TM-score. Nucleic acids research, 33(7), 2302-2309.'>approximated</abbr> if not a part of the input.</small>"
            },
            "AlphaTrace": {
                type: "AlphaTrace",
                name: "C-α Trace",
                description: "Render C-α trace of the molecule.<small><br/>HET atoms are optionally rendered as Balls and Sticks."
            }
        };

        this.yesNo = ["Yes", "No"];
        this.displayDetails = ["Very Low", "Low", "Medium", "High", "Very High"];
        
        this.groupingNames = _.filter(result.dataModel.partitionNames, function (n) { return n !== "AtomTypes"; });

        var inputState = {
            partitionName: ko.observable(null),

            chargesName: ko.observable(null),
            showDifference: ko.observable(false),
            chargesDiffName: ko.observable(null),

            displayMode: ko.observable(displayModes.BallsAndSticks),
            ballScaling: ko.observable(this.yesNo[0]),
            cartoonsShowHet: ko.observable("Yes"),
            cartoonsShowWaters: ko.observable("No"),
            probeRadius: ko.observable((0.5).toString()),
            displayDetail: ko.observable(this.displayDetails[2]),
            customMinValue: ko.observable(false),
            customMaxValue: ko.observable(false),
            minValue: ko.observable(-1),
            maxValue: ko.observable(1),
            minColor: ko.observable("#ff0000"),
            midColor: ko.observable("#ffffff"),
            maxColor: ko.observable("#0000ff"),
            valueGrouping: ko.observable(false),
            valueGroupingRadius: ko.observable((0.5).toString())
        };

        this.displayModes = ko.observable([displayModes.BallsAndSticks, displayModes.Surface, displayModes.Cartoons, displayModes.AlphaTrace, displayModes.VDWSpheres]);
        inputState.partitionName.subscribe(function (name) {
            if (name === "Atoms") {
                self.displayModes([displayModes.BallsAndSticks, displayModes.Surface, displayModes.Cartoons, displayModes.AlphaTrace, displayModes.VDWSpheres]);
            } else {
                self.displayModes([displayModes.BallsAndSticks, displayModes.Surface]);
            }
        });

        
        function snapState() {
            return _.mapValues(inputState, function (v) { return v(); });
        }
        
        var defaultState = snapState();

        
        this.setDefaultParameters = function () {
            var ignored = {
                partitionName: true,
                chargesName: true,
                showDifference: true,
                chargesDiffName: true
            };

            _.forEach(inputState, function (p, n) {
                if (ignored[n]) return;
                p(defaultState[n]);
            })
        };

        var $dummyBg = $("<div>");

        function validateColor(color) {
            color = color.toLowerCase();
            
            if (color.trim().length === 0) return false;

            $dummyBg.css('backgroundColor', 'white');
            $dummyBg.css('backgroundColor', color);
            if ($dummyBg.css('backgroundColor') !== 'rgb(255, 255, 255)' || color === 'white' || color === '#fff' || color === '#ffffff') {
                return true;
            }
            return false;
        }

        function validateState(state) {
            var errors = [], radius;
            if (state.customMinValue && isNaN(+state.minValue)) {
                errors.push("Min. Value must be a number.");
            }
            if (state.customMaxValue && isNaN(+state.maxValue)) {
                errors.push("Max. Value must be a number.");
            }
            if (!validateColor(state.minColor)) {
                errors.push("Min. Value color is not valid.");
            }
            if (!validateColor(state.midColor)) {
                errors.push("Mid. Value color is not valid.");
            }
            if (!validateColor(state.maxColor)) {
                errors.push("Max. Value color is not valid.");
            }
            if (state.showDifference && state.chargesName === state.chargesDiffName) {
                errors.push("For visualizing relative differences, please choose two distinct sets of charges.");
            }
            if (state.valueGrouping) {
                radius = +state.valueGroupingRadius;
                if (isNaN(radius) || radius < 0.05 || radius > 1.0 || state.valueGroupingRadius.trim().length === 0) {
                    errors.push("Value grouping radius must be a number between 0.05 and 1.");
                }
            }
            if (state.displayMode === "Surface") {
                radius = +state.probeRadius;
                if (isNaN(radius) || radius < 0.0 || radius > 5.0 || state.probeRadius.trim().length === 0) {
                    errors.push("Probe radius must be a number between 0 and 5.");
                }
            }

            self.errors(errors.length > 0 ? errors : null);
            return errors.length === 0;
        }

        function getNormalizedState(state) {
            var normalized = _.extend({}, state),
                partitions = result.Partitions[state.partitionName],
                charges = result.Partitions[state.partitionName].Charges[state.chargesName].GroupCharges,
                chargesDiff = result.Partitions[state.partitionName].Charges[state.chargesDiffName].GroupCharges,
                min = Number.MAX_VALUE, max = Number.MIN_VALUE;

            normalized.minValue = +state.minValue;
            normalized.maxValue = +state.maxValue;
            normalized.probeRadius = +state.probeRadius;
            normalized.ballScaling = state.ballScaling === "Yes";
            normalized.cartoonsShowHet = state.cartoonsShowHet === "Yes";
            normalized.cartoonsShowWaters = state.cartoonsShowWaters === "Yes";
            normalized.minColor = $dummyBg.css('backgroundColor', state.minColor).css('backgroundColor');
            normalized.midColor = $dummyBg.css('backgroundColor', state.midColor).css('backgroundColor');
            normalized.maxColor = $dummyBg.css('backgroundColor', state.maxColor).css('backgroundColor');
            normalized.valueGroupingRadius = +state.valueGroupingRadius;

            if (!state.customMinValue || !state.customMaxValue) {
                if (state.showDifference) {
                    _.forEach(charges, function (c, id) {
                        var v = c - chargesDiff[id];
                        if (!isNaN(v)) {
                            min = Math.min(min, v);
                            max = Math.max(max, v);
                        }
                    });
                } else {
                    _.forEach(charges, function (c, id) {
                        if (!isNaN(c)) {
                            min = Math.min(min, c);
                            max = Math.max(max, c);
                        }
                    });
                }

                if (min > 0 || min === Number.MAX_VALUE) {
                    min = 0.0;
                    normalized.minColor = normalized.midColor;
                }
                if (max < 0 || max === Number.MIN_VALUE) {
                    max = 0.0;
                    normalized.maxColor = normalized.midColor;
                }
                
                if (!state.customMinValue) normalized.minValue = min;
                if (!state.customMaxValue) normalized.maxValue = max;
            }

            return normalized;
        }

        function convertColor(color, defColor) {
            var regex = /rgb\([^\d]*(\d+)[^\d]*,[^\d]*(\d+)[^\d]*,[^\d]*(\d+)[^\d]*\)/i,
                match = regex.exec(color);

            if (!match) return defColor;
            return { r: +match[1] / 255.0, g: +match[2] / 255.0,  b: +match[3] / 255.0 }
            
        }
        
        function getUpdateType(oldState, newState) {
            if (!oldState) return "Draw";
            
            if (oldState.partitionName !== newState.partitionName 
                || oldState.displayMode !== newState.displayMode
                || oldState.displayDetail !== newState.displayDetail
                || (newState.displayMode.type === "Surface" && oldState.probeRadius !== newState.probeRadius)
                || ((newState.displayMode.type === "Cartoons" || newState.displayMode.type === "AlphaTrace") && (oldState.cartoonsShowHet !== newState.cartoonsShowHet || oldState.cartoonsShowWaters !== newState.cartoonsShowWaters))) {
                return "Draw";
            }

            /*if ((newState.displayMode.type === "BallsAndSticks" && oldState.ballScaling !== newState.ballScaling)
                || oldState.minValue !== newState.minValue
                || oldState.customMinValue !== newState.customMinValue
                || oldState.maxValue !== newState.maxValue
                || oldState.customMaxValue !== newState.customMaxValue
                || oldState.valueGrouping !== newState.valueGrouping
                || (newState.valueGrouping && oldState.valueGroupingRadius !== newState.valueGroupingRadius)) {
                return "UpdateTheme";
            }*/

            return "Theme";
        }

        function shouldUpdateMolecule(oldState, newState) {
            if (!oldState || oldState.partitionName !== newState.partitionName) return true;
            return false;
        }
                
        this.inputState = inputState;
        this.normalizedState = ko.observable(null);
        this.errors = ko.observable(null);

        this.is3NotAvailable = ko.observable(false);
        
        this.showUpdate = ko.observable(true);
        this.isBusy = ko.observable(true);

        var scene, structure,
            visualizedState = null,
            isAtomsGrouping = true,
            currentAtomElement = null,
            currentDrawing = null,
            currentCharges = {},
            currentDiffCharges = {},
            currentMolecule = null,
            atomsGroupingMap = {},
            firstDraw = true;

        function handleColors(property, target) {
            var picker = $(target).colorPicker({
                onColorChange: function (id, val) { property(val) },
                pickerDefault: property().substr(1)
            });
            property.subscribe(function (value) {
                picker.val(value).change();
            });
        }

        // for lazy initialization (element could be hidden when the page is loaded.).
        this.init = function () {
            try {
                handleColors(inputState.minColor, "#charges-3d-min-color");
                handleColors(inputState.midColor, "#charges-3d-mid-color");
                handleColors(inputState.maxColor, "#charges-3d-max-color");

                var bgPicker = $("#charges-3d-bg-color").colorPicker({
                    onColorChange: function (id, val) {
                        $("#details-3d-model-host")[0].style.background = val;
                    },
                    pickerDefault: "000"
                });

                _.forEach(result.Partitions.Atoms.Groups, function (g) {
                    atomsGroupingMap[g.AtomIds[0]] = g.Id;
                });

                if (!LiteMol.checkWebGL()) {
                    self.is3NotAvailable(true);
                    return;
                }

                scene = new LiteMol.Scene('details-3d-model-host');
                scene.events.addEventListener('hover', handleMouseHover);
                structure = JSON.parse(result.StructureJson);
                
                var atomResidueMap = {};
                _.forEach(structure.Residues, function (r) {
                    _.forEach(r.Atoms, function (id) {
                        atomResidueMap[id] = r;
                    });
                });
                structure.atomResidueMap = atomResidueMap;
                
                currentAtomElement = document.getElementById("charges-current-atom-info");

                if (result.atomCount < 20000) {
                    setTimeout(function () { self.update(true); }, 450);
                }

            } catch(e) {                
                self.is3NotAvailable(true);
                throw e;
            } finally {
                self.isBusy(false);
            }
        };

        this.resetCamera = function () {
            if (scene) {
                scene.resetCamera();
            }            
        };

        var infoLabel = "";
        function handleMouseHover(event) {
            if (!currentAtomElement) return;

            var data = event.data;
            if (data && data.element) {               
                var mainVal = currentCharges[data.element.Id],
                    diffVal = currentDiffCharges[data.element.Id],
                    val, lbl, res;

                if (visualizedState.showDifference) {
                    val = mainVal - diffVal;
                } else {
                    val = mainVal;
                }

                if (isNaN(val)) {
                    val = "Not Computed";
                } else {

                    if (visualizedState.showDifference) {
                        val = "" + mainVal.toFixed(5) + " vs. " + diffVal.toFixed(5) + " = " + val.toFixed(5);
                    } else {
                        val = val.toFixed(5);
                    }
                }

                if (data.element.Label) {
                    lbl = data.element.Label;
                } else if (data.element.Symbol) {

                    if (atomsGroupingMap[data.element.Id] !== undefined) {
                        lbl = result.Partitions.Atoms.Groups[atomsGroupingMap[data.element.Id]].Label;
                    } else {
                        if (data.element.Name) {
                            lbl = data.element.Name + " ";
                        } else {
                            lbl = "";
                        }
                        lbl += data.element.Symbol;
                        lbl += " " + data.element.Id;

                        if (structure.atomResidueMap && (res = structure.atomResidueMap[data.element.Id])) {
                            lbl += " (" + res.Name + " " + res.SerialNumber;
                            if (res.Chain !== '' && res.Chain !== ' ') lbl += " " + res.Chain;
                            lbl += ")";
                        }
                    }
                } else {
                    lbl = data.element.Name + " " + data.element.SerialNumber;
                    if (data.element.Chain.trim().length > 0) lbl += " " + data.element.Chain;
                    lbl += " (" + data.element.Atoms.length + " atoms)";

                    var sum = 0.0, diffSum = 0.0;
                    _.forEach(data.element.Atoms, function(id) {
                        if (visualizedState.showDifference) {
                            diffSum += currentDiffCharges[id];
                        }
                        sum += currentCharges[id];
                    });

                    if (isNaN(sum) || isNaN(diffSum)) {
                        val = "Not Computed";
                    } else {

                        if (visualizedState.showDifference) {
                            val = "" + sum.toFixed(5) + " vs. " + diffSum.toFixed(5) + " = " + (sum - diffSum).toFixed(5);
                        } else {
                            val = sum.toFixed(5);
                        }
                    }
                }

                var label = _.escape(lbl + ": " + val);
                
                if (infoLabel !== label) {
                    infoLabel = label;
                    currentAtomElement.innerHTML = label;
                }
            } else {
                if (infoLabel !== "") {
                    infoLabel = "";
                    currentAtomElement.innerHTML = "";
                }
            }
        }

        function makeMoleculeFromPartition(partition) {
            var positions = {};
            var getPosition = (function (structure, positions) {
                return function (group) {
                    var pos = positions[group.Id];
                    if (pos) return pos;

                    pos = [0, 0, 0];
                    _.forEach(group.AtomIds, function (id) {
                        var t = structure.Atoms[id].Position;
                        pos[0] += t[0]; pos[1] += t[1]; pos[2] += t[2];
                    });
                    pos[0] /= group.AtomIds.length; pos[1] /= group.AtomIds.length; pos[2] /= group.AtomIds.length;
                    positions[group.Id] = pos;
                    return pos;
                };
            })(structure, positions);

            function getBondsBetweenGroups(allBonds, groups) {
                function getBondHash(aId, bId) {
                    if (aId <= bId) return aId.toString() + "-" + bId.toString();
                    return bId.toString() + "-" + aId.toString();
                }

                var labels = {};
                _.forEach(groups, function(g) {
                    _.forEach(g.AtomIds, function (aId) {
                        labels[aId] = g.Id;
                    });
                });

                var knownBonds = {};
                var newBonds = [];
                _.forEach(allBonds, function (bond) {
                    var aId = labels[bond.A],
                        bId = labels[bond.B];
                    if (aId != bId) {
                        var bondHash = getBondHash(aId, bId);
                        if (!knownBonds[bondHash]) {
                            knownBonds[bondHash] = true;
                            newBonds.push({ aId: aId, bId: bId });
                        }
                    }
                });
                return newBonds;
            };

            var bondsBox = { bonds: null };
            var getBonds = (function (bondsBox, allBonds, groups, getter) {
                return function () {
                    if (bondsBox.bonds) return bondsBox.bonds;
                    bondsBox.bonds = getter(allBonds, groups);
                    return bondsBox.bonds;
                }
            })(bondsBox, structure.Bonds, partition.Groups, getBondsBetweenGroups);

            return new LiteMol.Molecule(partition, {
                getAtoms: function (data) { return data.Groups; },
                getAtomId: function (atom) { return atom.Id; },
                getAtomById: function (data, id) { return data.Groups[id]; },
                getAtomPosition: getPosition,
                getAtomRadius: (function (structure) {
                    return function (atom) {
                        if (atom.AtomIds.length === 1) {
                            return LiteMol.MoleculeHelpers.getRadiusFromSymbol(structure.Atoms[atom.AtomIds[0]].Symbol);
                        }
                        return 3.0;
                    };
                })(structure),
                getAtomSymbol: function (atom) { return "C"; },
                getBonds: getBonds,
                getBondInfo: function (b) { return b; }
            });
        }

        var secondaryStructureBox = { structure: undefined };

        function makeMolecule() {

            var getSecondaryStructure = (function (box) {
                return function (data) {
                    if (box.structure) return box.structure;
                    var secondary = [];
                    _.forEach(data.Helices, function (s) {
                        secondary.push({
                            type: LiteMol.SecondaryStructureType.Helix,
                            startResidue: { chain: s.StartResidue.Chain, serialNumber: s.StartResidue.SerialNumber },
                            endResidue: { chain: s.EndResidue.Chain, serialNumber: s.EndResidue.SerialNumber }
                        });
                    });
                    _.forEach(data.Sheets, function (s) {
                        secondary.push({
                            type: LiteMol.SecondaryStructureType.Sheet,
                            startResidue: { chain: s.StartResidue.Chain, serialNumber: s.StartResidue.SerialNumber },
                            endResidue: { chain: s.EndResidue.Chain, serialNumber: s.EndResidue.SerialNumber }
                        });
                    });
                    box.structure = secondary;
                    return secondary;
                };
            })(secondaryStructureBox);

            return new LiteMol.Molecule(structure, {
                getAtoms: function (data) { return data.Atoms; },
                getAtomId: function (atom) { return atom.Id; },
                getAtomById: function (data, id) { return data.Atoms[id]; },
                getAtomPosition: function (atom) { return atom.Position; },
                getAtomName: function (atom) { return atom.Name ? atom.Name : atom.Symbol; },
                getAtomRadius: function (atom) { return LiteMol.MoleculeHelpers.getRadiusFromSymbol(atom.Symbol) },
                getAtomSymbol: function (atom) { return atom.Symbol; },
                isAtomHet: function (atom) { return atom.RecordType !== "ATOM"; },

                getBonds: function (data) { return data.Bonds; },
                getBondInfo: function (b) { return { aId: b.A, bId: b.B }; },

                getSortedResidues: function (data) { return data.Residues; },
                getResidueAtomIds: function (residue) { return residue.Atoms; },
                getResidueId: function (residue) {
                    return { chain: residue.Chain, serialNumber: residue.SerialNumber, insertionCode: residue.InsertionCode };
                },
                getResidueName: function (residue) { return residue.Name || "UNK"; },

                getSecondaryStructure: getSecondaryStructure
            });
        }

        function makeTheme(state) {

            var minColor = convertColor(state.minColor, { r: 0, g: 0, b: 1 }),
                midColor = convertColor(state.midColor, { r: 1, g: 1, b: 1 }),
                maxColor = convertColor(state.maxColor, { r: 1, g: 0, b: 0 });
            
            var theme = new LiteMol.Themes.ChargeColoringTheme(currentMolecule.getters, currentCharges, state.showDifference ? currentDiffCharges : null,
                state.minValue, state.maxValue, state.ballScaling, state.valueGrouping, state.valueGroupingRadius, {
                minColor: minColor,
                midColor: midColor,
                maxColor: maxColor
            });

            return theme;
        }

        function updateTheme(state) {
            if (!currentDrawing || !currentMolecule) return;

            var theme = makeTheme(state);                        
            currentDrawing.applyTheme(theme);
            scene.forceRender();
        }

        function getDensity(state) {
            if (state.displayDetail === "Very Low") return 1.1;
            else if (state.displayDetail === "Low") return 1.33;
            else if (state.displayDetail === "Medium") return 1.77;
            else if (state.displayDetail === "High") return 2.2;
            else return 3;
        }

        function getTessalation(state) {
            if (state.displayDetail === "Very Low") return 0;
            else if (state.displayDetail === "Low") return 1;
            else if (state.displayDetail === "Medium") return 2;
            else if (state.displayDetail === "High") return 3;
            else return 4;
        }

        function updateDrawing(state) {
            if (currentDrawing) {
                scene.removeAndDisposeDrawing(currentDrawing);
                currentDrawing = null;
            }

            var theme = makeTheme(state);
                        
            if (state.displayMode.type === "Surface") {
                currentDrawing = LiteMol.Modes.MolecularSurfaceDrawing.create(currentMolecule, state.probeRadius, getDensity(state), theme);
            } else if (state.displayMode.type === "BallsAndSticks") {
                currentDrawing = LiteMol.Modes.BallsAndSticksDrawing.create(currentMolecule, getTessalation(state), LiteMol.Modes.BallsAndSticksDrawingStyle.BallsAndSticks, theme);
            } else if (state.displayMode.type === "VDWSpheres") {
                currentDrawing = LiteMol.Modes.BallsAndSticksDrawing.create(currentMolecule, getTessalation(state), LiteMol.Modes.BallsAndSticksDrawingStyle.VdwSpheres, theme);
            } else if (state.displayMode.type === "Cartoons") {
                currentDrawing = LiteMol.Modes.CartoonsDrawing.create(currentMolecule, getTessalation(state), theme, new LiteMol.Modes.CartoonsDrawingParameters(state.cartoonsShowHet, state.cartoonsShowWaters));
            } else if (state.displayMode.type === "AlphaTrace") {
                currentDrawing = LiteMol.Modes.CartoonsDrawing.create(currentMolecule, getTessalation(state), theme,
                    new LiteMol.Modes.CartoonsDrawingParameters(state.cartoonsShowHet, state.cartoonsShowWaters, LiteMol.Modes.CartoonsDrawingType.AlphaTrace));
            }
            scene.addDrawing(currentDrawing, firstDraw);
            firstDraw = false;
        }

        this.update = function (noHideIntro) {
            if (!scene || self.isBusy()) {
                return;
            }

            if (!noHideIntro && document.getElementById('details-3d-experimental')) {
                $('#details-3d-experimental').remove();
            }

            var newState = snapState();

            if (!validateState(newState)) return;
            self.errors(null);

            if (newState.partitionName === "Atoms") {
                currentCharges = result.Charges[newState.chargesName].Values;
                currentDiffCharges = result.Charges[newState.chargesDiffName].Values;
            } else {
                currentCharges = result.Partitions[newState.partitionName].Charges[newState.chargesName].GroupCharges;
                currentDiffCharges = result.Partitions[newState.partitionName].Charges[newState.chargesDiffName].GroupCharges;
            }
            
            try {
                if (shouldUpdateMolecule(visualizedState, newState)) {
                    if (newState.partitionName === "Atoms") {
                        currentMolecule = makeMolecule();
                    } else {
                        currentMolecule = makeMoleculeFromPartition(result.Partitions[newState.partitionName]);
                    }
                }
            } catch (e) {
                self.errors(["Error building molecule representation."]);
                throw e;
            }
            
            var normalizedState = getNormalizedState(newState),
                updateType = getUpdateType(visualizedState, newState);

            self.isBusy(true);
            try {
                if (updateType === "Draw") {
                    updateDrawing(normalizedState);
                } else {
                    updateTheme(normalizedState);
                }
                self.normalizedState(normalizedState);
                visualizedState = newState;
                self.showUpdate(false);
            } catch (e) {
                self.errors(["Oops, something went terribly wrong."]);
                throw e;
            } finally {
                self.isBusy(false);
            }
        };

        function stateUpdated() {
            var state = snapState();
            if (!validateState(state)) {
                self.showUpdate(false);
                return;
            }
            
            var type = getUpdateType(visualizedState, state);
            if (type === "Draw" || type === "UpdateTheme") {
                self.showUpdate(true);
            } else {
                self.showUpdate(false);
                self.update();
            }
        }

        var stateUpdatedWrapper = _.debounce(stateUpdated, 250, { trailing: true });
        _.forEach(inputState, function (p) { p.subscribe(stateUpdatedWrapper) });
    }

    result.view3d = new Visualizer(result, "details-3d-model");
}