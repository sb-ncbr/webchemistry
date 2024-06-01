function PatternQueryExplorer3DModelViewModel(vm, visualizerHost, wrap) {
    "use strict";

    var self = this,
        displayMode = 'Ball and Stick',
        showLabels = false,
        isWebGlAvailable = true,
        hostElement = document.getElementById(visualizerHost),
        wrapElement = document.getElementById(wrap);

    var missingWebGlMessage = "WebGL support is required to display the pattern.<br/>Learn more at <a href='//get.webgl.org/' target='_blank'>//get.webgl.org/</a>.";

    this.message = ko.observable("");

    function checkWebGL() {
        if (!window.WebGLRenderingContext /*|| isIE*/) {
            self.message(missingWebGlMessage);
            isWebGlAvailable = false;
            return;
        }
        var canvas = $("<canvas />").get(0);
        var gl;
        try { gl = canvas.getContext("webgl"); }
        catch (x) { gl = null; }
        if (gl === null) {
            try { gl = canvas.getContext("experimental-webgl"); }
            catch (x) { gl = null; }
        }
        if (gl === null) {
            isWebGlAvailable = false;
            self.message(missingWebGlMessage);
        }
    }

    checkWebGL();

    var visualizer = null,
        pickPos = { x: 0.0, y: 0.0 },
        currentPickContent = "",
        isMouseDown = false,
        pickInfoTarget = document.getElementById("mq-explorer-3d-atom-info");
    
    function pickAtom() {
        if (isMouseDown || !visualizer || !pickInfoTarget) {
            if (currentPickContent !== "") {
                currentPickContent = "";
                pickInfoTarget.textContent = "";
            }
            return;
        }

        var atom = visualizer.pick(pickPos.x, pickPos.y, true, false);
        if (!atom) {
            if (currentPickContent !== "") {
                currentPickContent = "";
                pickInfoTarget.textContent = "";
            }
            return;
        }
        if (currentPickContent !== atom.desc) {
            currentPickContent = atom.desc;
            pickInfoTarget.textContent = atom.desc;
        }
    }

    window.addEventListener('mouseup', function (e) { isMouseDown = false; });

    function createVisualizer() {
        if (visualizer !== null) return;
        
        var transform = new ChemDoodle.TransformCanvas3D(visualizerHost, wrapElement.clientWidth, wrapElement.clientHeight);
        if (!transform.gl) {
            self.message(missingWebGlMessage);
            isWebGlAvailable = false;
            visualizer = null;
            return;
        }
        transform.specs.atoms_displayLabels_3D = showLabels;
        transform.specs.atoms_useJMOLColors = true;
        transform.specs.projectionWidthHeightRatio_3D = wrapElement.clientWidth / wrapElement.clientHeight;
        transform.specs.set3DRepresentation(displayMode);
        transform.specs.backgroundColor = 'white';
        transform.specs.proteins_displayRibbon = false;
        transform.specs.proteins_displayBackbone = false;
        transform.specs.nucleics_display = false;
        transform.specs.macro_displayAtoms = true;
        transform.specs.macro_displayBonds = true;
        transform.specs.atoms_resolution_3D = 20;
        transform.specs.bonds_resolution_3D = 12;
        visualizer = transform;

        var picker = mqResultUtils.throttle(pickAtom, 17);
        hostElement.addEventListener('mousemove', function (e) {
            pickPos.x = e.layerX;
            pickPos.y = e.layerY;
            picker();
        });
        hostElement.addEventListener('mousedown', function (e) { isMouseDown = true; });
        hostElement.addEventListener('mouseup', function (e) { isMouseDown = false; });
    }
    
    this.toggleMode = function () {
        if (visualizer === null) return;

        if (displayMode === 'Ball and Stick') {
            displayMode = "Wireframe";
        } else {
            displayMode = 'Ball and Stick';
        }

        visualizer.specs.set3DRepresentation(displayMode);
        visualizer.setupScene();
        visualizer.repaint();
    };

    this.toggleLabels = function () {
        if (visualizer === null) return;

        showLabels = !showLabels;

        visualizer.specs.atoms_displayLabels_3D = showLabels;
        visualizer.repaint();
    };

    function showMotif(motif) {
        if (visualizer === null) return;
        if (!motif) {
            visualizer.loadContent([], []);
            return;
        }

        try
        {
            var data = JSON.parse(motif.SourceJson), mol = mqResultUtils.jsonToChemDoodleMolecule(data);
            visualizer.loadContent([mol], []);
        } catch (e) {
            visualizer.loadContent([], []);
            self.message("Error: " + e);
        }
    }

    this.updateSize = function () {
        if (visualizer === null) return;
        var w = wrapElement.clientWidth, h = wrapElement.clientHeight;
        visualizer.specs.projectionWidthHeightRatio_3D = w / h;
        visualizer.resize(w, h);
    };

    if (isWebGlAvailable) {
        createVisualizer();
        vm.motifs.currentMotif.subscribe(showMotif);

        var resize = mqResultUtils.throttle(self.updateSize, 100);
        $(window).resize(resize);
        hostElement.onmousedown = function () {
            vm.query.blur();
        };
    }
}