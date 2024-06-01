function PatternQuery3DModelViewModel(vm) {
    "use strict";

    var self = this,
        displayMode = 'Ball and Stick',
        showLabels = false,
        isWebGlAvailable = true,
        $desc, descEl;
        
    var missingWebGlMessage = "WebGL support is required to display the motif. Learn more at <a href='//get.webgl.org/' target='_blank'>//get.webgl.org/</a>.";

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

    function __DataProvider() {
        var cache = {}, xhr = null, timeout = null;

        this.getMotifSourceAsync = function (id, onSuccess, onError) {
            if (xhr !== null) {
                xhr.abort();
                xhr = null;
            }

            if (timeout !== null) {
                clearTimeout(timeout);
                timeout = null;
            }

            var key = vm.id + "::" + id;
            if (cache[key]) {
                setTimeout((function (data) { return function () { onSuccess(data); }; })(cache[key]), 50);
                return;
            } else {
                xhr = $.ajax({
                    url: PatternQueryActions.motifSourceProvider(vm.fullId, id, 'json', 'view'),
                    type: 'GET',
                    dataType: 'text'
                })
                .done(function (data) {
                    xhr = null;
                    cache[key] = data;
                    onSuccess(data);
                })
                .fail(function (jqXHR, textStatus, errorThrown) {
                    xhr = null;
                    if (jqXHR.status === 0 || jqXHR.readyState === 0 || errorThrown === "abort") return;
                    console.log(errorThrown);
                    onError(errorThrown);
                });
            }
        };
    }

    var dataProvider = new __DataProvider(),
        visualizer = null,
        pickPos = { x: 0.0, y: 0.0 },
        currentPickContent = "",
        isMouseDown = false;

    function pickAtom() {
        if (isMouseDown || !visualizer || !descEl) {
            if (currentPickContent !== "") {
                currentPickContent = "";
                descEl.textContent = "";
            }
            return;
        }

        var atom = visualizer.pick(pickPos.x, pickPos.y, true, false);
        if (!atom) {
            if (currentPickContent !== "") {
                currentPickContent = "";
                descEl.textContent = "";
            }
            return;
        }
        if (currentPickContent !== atom.desc) {
            currentPickContent = atom.desc;
            descEl.textContent = atom.desc;
        }
    }

    var picker = mqResultUtils.throttle(pickAtom, 17);
    var hostMouseMove = function (e) {
        pickPos.x = e.layerX;
        pickPos.y = e.layerY;
        picker();
    };
    var hostMouseDown = function (e) { isMouseDown = true; };
    var hostMouseUp = function (e) { isMouseDown = false; };

    window.addEventListener('mouseup', hostMouseUp);

    function createVisualizer() {
        if (visualizer !== null) return;

        var wrap = $('#' + vm.id + '-details-data .mq-motif-3d-host'),
            id = vm.id + "-motif-3d",
            target = $("<canvas id='" + id + "' />");
        wrap.append(target);
        
        var transform = new ChemDoodle.TransformCanvas3D(id, wrap.width(), wrap.height());
        if (!transform.gl) {
            target.remove();
            self.message(missingWebGlMessage);
            isWebGlAvailable = false;
            visualizer = null;
            return;
        }
        transform.specs.atoms_displayLabels_3D = showLabels;
        transform.specs.atoms_useJMOLColors = true;
        transform.specs.projectionWidthHeightRatio_3D = wrap.width() / wrap.height();
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

        $desc = $('#' + vm.id + '-details-data .mq-motif-3d-desc');
        descEl = $desc[0];

        pickPos.x = 0;
        pickPos.y = 0;
        isMouseDown = false;        
        target[0].addEventListener('mousemove', hostMouseMove);
        target[0].addEventListener('mousedown', hostMouseDown);
        target[0].addEventListener('mouseup', hostMouseUp);
    }
    
    function destroyVisualizer() {
        if (visualizer === null) return;
        var canvas = $('#' + vm.id + '-details-data .mq-motif-3d-wrap canvas');

        if (canvas[0]) {
            canvas[0].removeEventListener('mousemove', hostMouseMove);
            canvas[0].removeEventListener('mousedown', hostMouseDown);
            canvas[0].removeEventListener('mouseup', hostMouseUp);
        }

        canvas.empty().remove();
        visualizer = null;
    }

    
    this.isLarge = ko.observable(false);
    this.isLarge.subscribe(function (large) {
        var //panel = $('#' + vm.id + '-3d-view'),
            wrap = $('#' + vm.id + '-details-data .mq-motif-3d-wrap'),
            host = $('#' + vm.id + '-details-data .mq-motif-3d-host'),
            overlay = $('#' + vm.id + '-details-data .mq-motif-3d-large-overlay');
        
        if (large) {
            //wrap.remove().appendTo($("body"));
            wrap.removeClass('mq-motif-3d-wrap-small').addClass('mq-motif-3d-wrap-large');
            overlay.show();
            //$("body").css("overflow", "hidden");
            //$('#' + vm.id + '-details-data .tabbable').css("overflow", "hidden");
        } else {
            wrap.removeClass('mq-motif-3d-wrap-large').addClass('mq-motif-3d-wrap-small');
            overlay.hide();
            setTimeout(function () { wrap.removeClass('mq-motif-3d-wrap-small'); }, 50);
            //$("body").css("overflow", "auto");
            //$('#' + vm.id + '-details-data .tabbable').css("overflow", "auto");
        }
        
        var w = host.width(), h = host.height();
        visualizer.specs.projectionWidthHeightRatio_3D = w / h;
        visualizer.resize(w, h);
    });

    this.src = ko.observable("");

    this.toggleSize = function () {
        var large = !self.isLarge();
        self.isLarge(large);
    };

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

    $(document).keyup((function (isLarge) { return function (e) { if (e.keyCode === 27) { isLarge(false); } }; })(this.isLarge));
    
    this.init = function () {
        $('#' + vm.id + '-details-data .mq-motif-3d-large-overlay').click((function (isLarge) { return function () { isLarge(false); }; })(this.isLarge));
    };

    this.show = function () {
        createVisualizer();
        if (vm.currentMotif()) showMotif(vm.currentMotif());
    };

    this.hide = function () {
        destroyVisualizer();
        shownMotif = null;
    };

    vm.currentMotif.subscribe(showMotif);

    //var molReader = new ChemDoodle.io.MOLInterpreter(),
    var shownMotif = null;

    function showMotif(motif) {
        if (!isWebGlAvailable || visualizer === null || shownMotif === motif) return;

        self.message("Loading...");
        dataProvider.getMotifSourceAsync(motif.Id,
            function (src) {
                try {
                    self.message("");
                    //var mol = molReader.read(src, 1);
                    var mol = mqResultUtils.jsonToChemDoodleMolecule(JSON.parse(src));
                    visualizer.loadContent([mol], []);
                } catch (e) {
                    self.message("Oops, something went wrong while displaying the pattern.");
                }
            },
            function (err) {
                self.message("Oops, error downloading the pattern.");
            });
    }
}