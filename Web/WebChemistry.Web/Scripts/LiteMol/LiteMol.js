var LiteMol;
(function (LiteMol) {
    var SceneOptions = (function () {
        function SceneOptions(options) {
            if (options === void 0) { options = {}; }
            this.alpha = true;
            _.extend(this, options, { cameraFOV: 60, color: { r: 0, g: 0, b: 0 } });
        }
        SceneOptions.Default = new SceneOptions();
        return SceneOptions;
    })();
    LiteMol.SceneOptions = SceneOptions;
    var MouseInfo = (function () {
        function MouseInfo() {
            this.position = { x: 0.0, y: 0.0 };
            this.lastPosition = { x: 0.0, y: 0.0 };
            this.exactPosition = { x: 0.0, y: 0.0 };
            this.isInside = false;
            this.isButtonDown = false;
        }
        MouseInfo.prototype.update = function () {
            if (this.lastPosition.x === this.position.x && this.lastPosition.y === this.position.y) {
                return false;
            }
            this.lastPosition.x = this.position.x;
            this.lastPosition.y = this.position.y;
            return true;
        };
        MouseInfo.prototype.setExactPosition = function (element) {
            var xPosition = 0, yPosition = 0;
            while (element) {
                xPosition += (element.offsetLeft - element.scrollLeft + element.clientLeft);
                yPosition += (element.offsetTop - element.scrollTop + element.clientTop);
                element = element.offsetParent;
            }
            this.exactPosition.x = this.position.x - xPosition;
            this.exactPosition.y = this.position.y - yPosition;
        };
        return MouseInfo;
    })();
    var PickInfo = (function () {
        function PickInfo() {
            this.current = null;
            this.currentPickId = -1;
            this.currentPickElementId = -1;
        }
        PickInfo.prototype.reset = function () {
            var changed = this.current !== null;
            this.currentPickId = -1;
            this.currentPickElementId = -1;
            this.current = null;
            return changed;
        };
        return PickInfo;
    })();
    var Scene = (function () {
        function Scene(elementId, options) {
            var _this = this;
            if (options === void 0) { options = SceneOptions.Default; }
            this.lastRenderTime = 0.0;
            this.pickDelta = 0.0;
            this.mouseInfo = new MouseInfo();
            this.pickInfo = new PickInfo();
            this.width = 0.0;
            this.height = 0.0;
            this.resizing = false;
            this.drawings = {};
            this.unbindEvents = [];
            this.events = new THREE.EventDispatcher();
            this.renderFunc = _.bind(this.render, this);
            this.pickBuffer = new Uint8Array(4);
            this.options = options;
            this.parentElement = document.getElementById(elementId);
            this.scene = new THREE.Scene();
            this.highlightScene = new THREE.Scene();
            this.pickScene = new THREE.Scene();
            this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: this.options.alpha });
            this.renderer.autoClear = false;
            this.renderer.autoClearDepth = false;
            this.pickTarget = new THREE.WebGLRenderTarget(Scene.pickTargetWidth, Scene.pickTargetHeight);
            this.pickTarget.generateMipmaps = false;
            this.pickRenderer = new THREE.WebGLRenderer({ antialias: false, alpha: true });
            this.pickRenderer.setSize(Scene.pickTargetWidth, Scene.pickTargetHeight);
            this.pickRenderer.setClearColor(new THREE.Color(0, 0, 0), 1.0);
            this.camera = new THREE.PerspectiveCamera(this.options.cameraFOV, this.parentElement.clientWidth / this.parentElement.clientHeight, 0.1, 10000);
            this.camera.position.set(0, 0, 500);
            this.cameraControls = new THREE.TrackballControls(this.camera, this.renderer.domElement);
            var cameraUpdated = _.bind(this.needsRender, this);
            this.cameraControls.addEventListener('change', cameraUpdated);
            this.unbindEvents.push(function () { return _this.cameraControls.removeEventListener('change', cameraUpdated); });
            if (!this.options.alpha) {
                this.renderer.setClearColor(new THREE.Color(this.options.color.r, this.options.color.g, this.options.color.b), 1);
            }
            this.setupLights();
            this.parentElement.appendChild(this.renderer.domElement);
            var delayedResizeHandler = _.debounce(_.bind(this.handleResize, this), 100), resizeHandler = function () {
                _this.resizing = true;
                delayedResizeHandler();
            };
            window.addEventListener('resize', resizeHandler);
            this.unbindEvents.push(function () { return window.removeEventListener('resize', resizeHandler); });
            this.setupMouse();
            this.handleResize();
            this.renderer.clear();
            this.needsRender();
            this.animationFrame = requestAnimationFrame(this.renderFunc);
        }
        Scene.prototype.setupMouse = function () {
            var _this = this;
            var handleMove = function (e) {
                _this.mouseInfo.position.x = e.clientX;
                _this.mouseInfo.position.y = e.clientY;
                _this.mouseInfo.isInside = true;
            };
            this.parentElement.addEventListener('mousemove', handleMove);
            this.unbindEvents.push(function () { return _this.parentElement.removeEventListener('mousemove', handleMove); });
            var handleLeave = function (e) {
                _this.mouseInfo.isInside = false;
            };
            this.parentElement.addEventListener('mouseleave', handleLeave);
            this.unbindEvents.push(function () { return _this.parentElement.removeEventListener('mouseleave', handleLeave); });
            var handleDown = function (e) {
                _this.clearHighlights();
                _this.mouseInfo.isButtonDown = true;
            };
            this.parentElement.addEventListener('mousedown', handleDown);
            this.unbindEvents.push(function () { return _this.parentElement.removeEventListener('mousedown', handleDown); });
            var handleUp = function (e) {
                _this.mouseInfo.isButtonDown = false;
            };
            window.addEventListener('mouseup', handleUp);
            this.unbindEvents.push(function () { return window.removeEventListener('mouseup', handleUp); });
            this.parentElement.addEventListener('mousewheel', _.bind(this.handleMouseWheel, this));
            this.parentElement.addEventListener('DOMMouseScroll', _.bind(this.handleMouseWheel, this));
            this.unbindEvents.push(function () {
                _this.parentElement.removeEventListener('mousewheel', handleDown);
                _this.parentElement.removeEventListener('DOMMouseScroll', handleDown);
            });
        };
        Scene.prototype.handleMouseWheel = function (event) {
            var delta = 0;
            if (event.wheelDelta) {
                delta = event.wheelDelta;
            }
            else if (event.detail) {
                delta = -event.detail;
            }
            if (delta < -3)
                delta = -3;
            else if (delta > 3)
                delta = 3;
            this.camera.near = Math.max(this.camera.near + delta, 0.1);
            this.camera.updateProjectionMatrix();
            this.needsRender();
        };
        Scene.prototype.dirLight = function (x, y, z, int) {
            var directionalLight = new THREE.DirectionalLight(0xffffff, int);
            directionalLight.position.set(x, y, z);
            this.scene.add(directionalLight);
            this.highlightScene.add(directionalLight.clone());
        };
        Scene.prototype.setupLights = function () {
            this.pointLight = new THREE.PointLight(0xffffff, 0.85);
            this.scene.add(this.pointLight);
            this.highlightPointLight = this.pointLight.clone();
            this.highlightScene.add(this.highlightPointLight);
            var ambient = new THREE.AmbientLight(0x777777);
            this.scene.add(ambient);
            this.highlightScene.add(ambient.clone());
        };
        Scene.prototype.handleResize = function () {
            var w = this.parentElement.clientWidth, h = this.parentElement.clientHeight;
            this.width = w;
            this.height = h;
            this.camera.aspect = w / h;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(w, h);
            this.cameraControls.handleResize();
            this.resizing = false;
            this.needsRender();
        };
        Scene.prototype.needsRender = function () {
            this.rendered = false;
            this.pickRendered = false;
        };
        Scene.prototype.clearHighlights = function (update) {
            if (update === void 0) { update = true; }
            var changed = false;
            _.forEach(this.drawings, function (d) {
                changed = changed || d.clearHighlight();
            });
            if (changed && update)
                this.needsRender();
            if (this.pickInfo.reset()) {
                this.dispatchHoverEvent();
            }
        };
        Scene.prototype.render = function (time) {
            if (this.resizing) {
                this.animationFrame = requestAnimationFrame(this.renderFunc);
                return;
            }
            var delta = time - this.lastRenderTime;
            this.pickDelta += delta;
            this.lastRenderTime = time;
            if (this.pickDelta > 33.3333333) {
                if (!this.pickRendered && this.mouseInfo.isInside && !this.mouseInfo.isButtonDown) {
                    this.pickRenderer.clear();
                    this.pickRenderer.render(this.pickScene, this.camera, this.pickTarget);
                    this.pickRendered = true;
                }
                this.pickDelta = this.pickDelta % 33.3333333;
                this.handlePick();
            }
            if (!this.rendered) {
                this.pointLight.position.copy(this.camera.position);
                this.highlightPointLight.position.copy(this.camera.position);
                this.renderer.clear(true, true, true);
                this.renderer.render(this.scene, this.camera);
                this.renderer.render(this.highlightScene, this.camera);
                this.rendered = true;
            }
            this.animationFrame = requestAnimationFrame(this.renderFunc);
        };
        Scene.prototype.dispatchHoverEvent = function () {
            this.events.dispatchEvent({ type: Scene.hoverEvent, target: null, data: this.pickInfo.current });
        };
        Scene.prototype.handlePick = function () {
            if (!this.mouseInfo.isInside || this.mouseInfo.isButtonDown || !this.mouseInfo.update()) {
                return;
            }
            JSON.stringify;
            this.mouseInfo.setExactPosition(this.parentElement);
            var gl = this.pickRenderer.getContext(), position = this.mouseInfo.exactPosition;
            gl.readPixels(position.x * Scene.pickTargetWidth / this.width, this.pickTarget.height - position.y * Scene.pickTargetHeight / this.height, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, this.pickBuffer);
            var id = this.pickBuffer[3], pickId = (this.pickBuffer[0] << 16) | (this.pickBuffer[1] << 8) | (this.pickBuffer[2]), info = this.pickInfo;
            if (id === info.currentPickId && pickId === info.currentPickElementId)
                return;
            info.currentPickId = id;
            info.currentPickElementId = pickId;
            var drawing = this.drawings[id];
            if (id === 255 || !drawing) {
                this.clearHighlights();
                return;
            }
            _.forEach(this.drawings, function (d) {
                if (d.sceneId !== id)
                    d.clearHighlight();
            });
            if (drawing.highlightElement(pickId)) {
                this.needsRender();
                info.current = drawing.getPickElement(pickId);
                this.dispatchHoverEvent();
            }
        };
        Scene.prototype.forceRender = function () {
            this.needsRender();
        };
        Scene.prototype.resetCamera = function () {
            var center = new THREE.Vector3(), drawingCount = _.size(this.drawings), count = 0;
            _.forEach(this.drawings, function (d) {
                if (d.includeInCentroidComputation) {
                    center.add(d.centroid);
                    count++;
                }
            });
            center.multiplyScalar(1 / Math.max(drawingCount, 1));
            var radius = _.max(_.map(this.drawings, function (d) { return d.includeInCentroidComputation ? new THREE.Vector3().subVectors(center, d.centroid).length() + d.radius : 0.0; }));
            this.camera.near = 0.1;
            this.camera.updateProjectionMatrix();
            if (count > 0) {
                this.camera.position.set(center.x, center.y, center.z - 2.0 * radius);
            }
            else {
                this.camera.position.set(0, 0, 500);
            }
            this.camera.lookAt(center);
            this.cameraControls.target = center;
            this.needsRender();
        };
        Scene.prototype.addDrawing = function (drawing, resetCamera) {
            if (resetCamera === void 0) { resetCamera = true; }
            var id = -1;
            var ids = _.map(_.keys(this.drawings), function (v) { return +v; });
            if (ids.length === 0) {
                id = 0;
            }
            else {
                var maxId = _.reduce(ids, function (max, x) { return Math.max(max, x); });
                for (var i = 0; i < maxId; i++) {
                    if (!this.drawings[i]) {
                        id = i;
                        break;
                    }
                }
                if (id === -1) {
                    id = maxId + 1;
                }
            }
            this.drawings[id] = drawing;
            drawing.addedToScene(id);
            if (drawing.object)
                this.scene.add(drawing.object);
            if (drawing.highlightObject)
                this.highlightScene.add(drawing.highlightObject);
            if (drawing.pickObject)
                this.pickScene.add(drawing.pickObject);
            if (resetCamera) {
                this.resetCamera();
            }
            else {
                this.needsRender();
            }
        };
        Scene.prototype.removeAndDisposeDrawing = function (drawing, update) {
            if (update === void 0) { update = true; }
            if (!this.drawings[drawing.sceneId])
                return;
            if (drawing.object)
                this.scene.remove(drawing.object);
            if (drawing.highlightObject)
                this.highlightScene.remove(drawing.highlightObject);
            if (drawing.pickObject)
                this.pickScene.remove(drawing.pickObject);
            drawing.dispose();
            delete this.drawings[drawing.sceneId];
            drawing.sceneId = -1;
            if (update)
                this.needsRender();
        };
        Scene.prototype.clear = function () {
            var _this = this;
            _.forEach(this.drawings, function (d) { return _this.removeAndDisposeDrawing(d, false); });
            this.needsRender();
        };
        Scene.prototype.destroy = function () {
            _.forEach(this.unbindEvents, function (e) { return e(); });
            this.unbindEvents = [];
            cancelAnimationFrame(this.animationFrame);
            this.scene = null;
            this.highlightScene = null;
            this.pickScene = null;
            this.camera = null;
            this.cameraControls.destroy();
            this.cameraControls = null;
            this.renderer = null;
            this.pickRenderer = null;
            this.pickTarget.dispose();
            this.pickTarget = null;
            while (this.parentElement.lastChild)
                this.parentElement.removeChild(this.parentElement.lastChild);
        };
        Scene.hoverEvent = 'hover';
        Scene.pickTargetWidth = 1024;
        Scene.pickTargetHeight = 1024;
        return Scene;
    })();
    LiteMol.Scene = Scene;
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var MoleculeWrapper = (function () {
        function MoleculeWrapper() {
        }
        return MoleculeWrapper;
    })();
    LiteMol.MoleculeWrapper = MoleculeWrapper;
    var Drawing = (function () {
        function Drawing() {
            this.disposeList = [];
            this.sceneId = -1;
            this.centroid = new THREE.Vector3();
            this.radius = 0;
            this.object = undefined;
            this.highlightObject = undefined;
            this.pickObject = undefined;
            this.includeInCentroidComputation = true;
            this.theme = undefined;
        }
        Drawing.prototype.applyTheme = function (theme) {
            this.theme = theme;
        };
        Drawing.prototype.addedToScene = function (id) {
            this.sceneId = id;
            this.addedToSceneInternal();
        };
        Drawing.prototype.dispose = function () {
            _.forEach(this.disposeList, function (d) { return d.dispose(); });
            this.disposeList = null;
            this.disposeList = [];
        };
        Drawing.prototype.addedToSceneInternal = function () {
        };
        Drawing.prototype.getPickElement = function (pickId) {
            throw "Not Implemented";
        };
        Drawing.prototype.highlightElement = function (pickId) {
            return false;
        };
        Drawing.prototype.clearHighlight = function () {
            return false;
        };
        return Drawing;
    })();
    LiteMol.Drawing = Drawing;
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var GeometryBase = (function () {
        function GeometryBase() {
        }
        GeometryBase.prototype.dispose = function () {
        };
        return GeometryBase;
    })();
    LiteMol.GeometryBase = GeometryBase;
    var GeometryHelper = (function () {
        function GeometryHelper() {
        }
        GeometryHelper.setPickColor = function (objectId, objectIdWidth, elementId, buffer, offset) {
            var width = 24, value = objectId << (width - objectIdWidth) | elementId, r = (value >> 16) & 0xFF, g = (value >> 8) & 0xFF, b = value & 0xFF;
            buffer[offset] = r / 255.0;
            buffer[offset + 1] = g / 255.0;
            buffer[offset + 2] = b / 255.0;
        };
        GeometryHelper.getIndexedBufferGeometry = function (source) {
            var bufferSize = source.vertices.length * 3, vertexBuffer = new Float32Array(bufferSize), normalBuffer = new Float32Array(bufferSize), indexBuffer = new Uint32Array(source.faces.length * 3), normals = Array(source.vertices.length);
            _.forEach(source.faces, function (f, i) {
                normals[f.a] = f.vertexNormals[0];
                normals[f.b] = f.vertexNormals[1];
                normals[f.c] = f.vertexNormals[2];
                indexBuffer[3 * i] = f.a;
                indexBuffer[3 * i + 1] = f.b;
                indexBuffer[3 * i + 2] = f.c;
            });
            _.forEach(source.vertices, function (v, i) {
                vertexBuffer[3 * i] = v.x;
                vertexBuffer[3 * i + 1] = v.y;
                vertexBuffer[3 * i + 2] = v.z;
                var n = normals[i];
                normalBuffer[3 * i] = n.x;
                normalBuffer[3 * i + 1] = n.y;
                normalBuffer[3 * i + 2] = n.z;
            });
            var geom = new THREE.BufferGeometry();
            geom.addAttribute('position', new THREE.BufferAttribute(vertexBuffer, 3));
            geom.addAttribute('normal', new THREE.BufferAttribute(normalBuffer, 3));
            geom.addAttribute('index', new THREE.BufferAttribute(indexBuffer, 1));
            return geom;
        };
        return GeometryHelper;
    })();
    LiteMol.GeometryHelper = GeometryHelper;
    var PickFlagInfo = (function () {
        function PickFlagInfo(value, width) {
            this.value = value;
            this.width = width;
        }
        PickFlagInfo.Empty = new PickFlagInfo(0, 0);
        return PickFlagInfo;
    })();
    LiteMol.PickFlagInfo = PickFlagInfo;
    var GeometryHighlighterBase = (function () {
        function GeometryHighlighterBase() {
            this.bufferSize = 0;
            this.indexBufferSize = 0;
            var arrays = {
                vertex: new Float32Array(0),
                normal: new Float32Array(0),
                index: new Uint32Array(0)
            }, buffers = {
                vertex: new THREE.BufferAttribute(new Float32Array(arrays.vertex.buffer, 0, 0), 3),
                index: new THREE.BufferAttribute(new Uint32Array(arrays.index.buffer, 0, 0), 1),
                normal: new THREE.BufferAttribute(new Float32Array(arrays.normal.buffer, 0, 0), 3)
            };
            this.arrays = arrays;
            this.buffers = buffers;
            var geometry = new THREE.BufferGeometry();
            geometry.addAttribute('position', buffers.vertex);
            geometry.addAttribute('normal', buffers.normal);
            geometry.addAttribute('index', buffers.index);
            this.material = LiteMol.MaterialsHelper.getDefaultHighlightMaterial();
            this.object = new THREE.Mesh(geometry, this.material);
        }
        GeometryHighlighterBase.prototype.isElementHighlighted = function (highlightId) {
            return (this.isHighlighted && this.highlightId === highlightId);
        };
        GeometryHighlighterBase.prototype.highlight = function (highlightId, color, params) {
            if (this.isHighlighted && highlightId === this.highlightId)
                return false;
            var matColor = this.material.color;
            if (matColor.r !== color.r || matColor.g !== color.g || matColor.b !== color.b) {
                matColor.r = color.r;
                matColor.g = color.g;
                matColor.b = color.b;
            }
            this.highlightInternal(params);
            this.isHighlighted = true;
            this.highlightId = highlightId;
            this.object.visible = true;
            this.buffers.vertex['needsUpdate'] = true;
            this.buffers.index['needsUpdate'] = true;
            this.buffers.normal['needsUpdate'] = true;
            return true;
        };
        GeometryHighlighterBase.prototype.highlightInternal = function (params) {
        };
        GeometryHighlighterBase.prototype.clear = function () {
            if (!this.isHighlighted)
                return false;
            this.isHighlighted = false;
            this.object.visible = false;
            return true;
        };
        GeometryHighlighterBase.prototype.ensureBuffersSize = function () {
            var forceUpdate = false;
            if (this.arrays.vertex.length > 3 * this.bufferSize && this.arrays.vertex.length > 1.5 * GeometryHighlighterBase.defaultBufferSize * 3 && this.bufferSize <= GeometryHighlighterBase.defaultBufferSize) {
                forceUpdate = true;
                this.bufferSize = GeometryHighlighterBase.defaultBufferSize;
            }
            if (forceUpdate || this.arrays.vertex.length < 3 * this.bufferSize) {
                this.arrays.vertex = new Float32Array(Math.max(this.bufferSize, GeometryHighlighterBase.defaultBufferSize) * 3);
                this.arrays.normal = new Float32Array(Math.max(this.bufferSize, GeometryHighlighterBase.defaultBufferSize) * 3);
            }
            forceUpdate = false;
            if (this.arrays.index.length > this.indexBufferSize && this.arrays.index.length > 1.5 * GeometryHighlighterBase.defaultIndexBufferSize && this.indexBufferSize <= GeometryHighlighterBase.defaultIndexBufferSize) {
                forceUpdate = true;
                this.indexBufferSize = GeometryHighlighterBase.defaultIndexBufferSize;
            }
            if (forceUpdate || this.arrays.index.length < this.indexBufferSize) {
                this.arrays.index = new Uint32Array(Math.max(this.indexBufferSize, GeometryHighlighterBase.defaultIndexBufferSize));
            }
        };
        GeometryHighlighterBase.prototype.dispose = function () {
            this.object.geometry.dispose();
            this.object.material.dispose();
        };
        GeometryHighlighterBase.defaultBufferSize = 1000;
        GeometryHighlighterBase.defaultIndexBufferSize = 5000;
        return GeometryHighlighterBase;
    })();
    LiteMol.GeometryHighlighterBase = GeometryHighlighterBase;
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    LiteMol.VERSION = "1.0.14.11.17";
    function checkWebGL() {
        return (function () {
            try {
                return !!window['WebGLRenderingContext'] && !!document.createElement('canvas').getContext('experimental-webgl');
            }
            catch (e) {
                return false;
            }
        })();
    }
    LiteMol.checkWebGL = checkWebGL;
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var MaterialsHelper = (function () {
        function MaterialsHelper() {
        }
        MaterialsHelper.getPickMaterial = function () {
            return new THREE.ShaderMaterial({
                attributes: { pColor: { type: 'v4', value: [] } },
                vertexShader: MaterialsHelper.pickVertexShader,
                fragmentShader: MaterialsHelper.pickFragmentShader,
                blending: THREE.NoBlending,
                shading: THREE.FlatShading,
                side: THREE.DoubleSide
            });
        };
        MaterialsHelper.getPickExcludeMaterial = function () {
            return new THREE.MeshBasicMaterial({ color: THREE.ColorKeywords.white, side: THREE.DoubleSide });
        };
        MaterialsHelper.getPhongVertexColorMaterial = function () {
            return new THREE.MeshPhongMaterial({ specular: 0xAAAAAA, ambient: 0xffffff, shininess: 2, shading: THREE.SmoothShading, vertexColors: THREE.VertexColors, side: THREE.DoubleSide, metal: true });
        };
        MaterialsHelper.getDefaultHighlightMaterial = function () {
            return new THREE.MeshPhongMaterial({ color: 0xFFFFFF, specular: 0xAAAAAA, ambient: 0xffffff, shininess: 2, shading: THREE.SmoothShading, side: THREE.DoubleSide, metal: true });
        };
        MaterialsHelper.pickVertexShader = [
            "attribute vec4 pColor;",
            "varying vec4 pC;",
            "void main() {",
            "pC = pColor;",
            "gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );",
            "}"
        ].join('\n');
        MaterialsHelper.pickFragmentShader = [
            "varying vec4 pC;",
            "void main() {",
            "gl_FragColor = pC;",
            "}"
        ].join('\n');
        return MaterialsHelper;
    })();
    LiteMol.MaterialsHelper = MaterialsHelper;
})(LiteMol || (LiteMol = {}));
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        var Geometry;
        (function (Geometry) {
            var BallsAndSticksGeometryAtomVertexInfo = (function () {
                function BallsAndSticksGeometryAtomVertexInfo() {
                    this.bondStarts = [];
                    this.bondStartCount = 0;
                    this.scale = 1.0;
                }
                BallsAndSticksGeometryAtomVertexInfo.prototype.addBondStart = function (s) {
                    this.bondStarts[this.bondStartCount++] = s;
                };
                return BallsAndSticksGeometryAtomVertexInfo;
            })();
            Geometry.BallsAndSticksGeometryAtomVertexInfo = BallsAndSticksGeometryAtomVertexInfo;
            var BallsAndSticksGeometry = (function (_super) {
                __extends(BallsAndSticksGeometry, _super);
                function BallsAndSticksGeometry(molecule, tessalation, isVdw, pickFlag) {
                    _super.call(this);
                    this.isVdw = isVdw;
                    var geometry = new THREE.BufferGeometry(), pickGeometry = new THREE.BufferGeometry(), cx = 0.0, cy = 0.0, cz = 0.0, radiusSq = 0.0;
                    var atoms = molecule.getAtoms(), bonds = molecule.getBonds(), getBondInfo = molecule.getters.getBondInfo, atomsInfo = {};
                    if (isVdw) {
                        bonds = [];
                        tessalation = Math.floor(1.4 * tessalation);
                    }
                    _.forEach(atoms, function (atom) {
                        atomsInfo[molecule.getters.getAtomId(atom)] = new BallsAndSticksGeometryAtomVertexInfo();
                    });
                    var bondsLengthSum = 0;
                    _.forEach(bonds, function (bondObject) {
                        var bond = getBondInfo(bondObject), posA = molecule.getters.getAtomPosition(molecule.getAtomById(bond.aId)), posB = molecule.getters.getAtomPosition(molecule.getAtomById(bond.bId));
                        bondsLengthSum += (new THREE.Vector3).subVectors(new THREE.Vector3().fromArray(posA), new THREE.Vector3().fromArray(posB)).length();
                    });
                    var bondCount = bonds.length, atomCount = Object.keys(atoms).length, templateScale = bondCount > 0 && bondsLengthSum > 0 ? bondsLengthSum / bondCount : 1.0;
                    var bondTemplate = this.getBondTemplate(isVdw ? 1.0 : templateScale * 0.05, tessalation), atomTemplate = this.getAtomTemplate(isVdw ? 1.0 : templateScale * 0.2, tessalation);
                    var bondTemplateVertexBuffer = bondTemplate.attributes['position'].array, bondTemplateVertexBufferLength = bondTemplateVertexBuffer.length, bondTemplateVertexCount = (bondTemplateVertexBufferLength / 3) | 0, bondTemplateIndexBuffer = bondTemplate.attributes['index'].array, bondTemplateIndexBufferLength = bondTemplateIndexBuffer.length, bondTemplateNormalBuffer = bondTemplate.attributes['normal'].array, atomTemplateVertexBuffer = atomTemplate.attributes['position'].array, atomTemplateVertexBufferLength = atomTemplateVertexBuffer.length, atomTemplateVertexCount = (atomTemplateVertexBufferLength / 3) | 0, atomTemplateIndexBuffer = atomTemplate.attributes['index'].array, atomTemplateIndexBufferLength = atomTemplateIndexBuffer.length, atomTemplateNormalBuffer = atomTemplate.attributes['normal'].array;
                    var bufferSize = bondTemplateVertexBufferLength * bondCount + atomTemplateVertexBufferLength * atomCount, vertexBuffer = new Float32Array(bufferSize), normalBuffer = new Float32Array(bufferSize), colorBuffer = new Float32Array(bufferSize), indexBuffer = new Uint32Array(bondTemplateIndexBufferLength * bondCount + atomTemplateIndexBufferLength * atomCount), pickVertexBuffer = new Float32Array(atomTemplateVertexBufferLength * atomCount), pickIndexBuffer = new Uint32Array(atomTemplateIndexBufferLength * atomCount), pickColorBuffer = new Float32Array(atomTemplateVertexCount * 4 * atomCount);
                    var atomsVector = new THREE.Vector3(), center = new THREE.Vector3(), rotationAxis = new THREE.Vector3(), up = new THREE.Vector3(0, 1, 0), rotationAngle;
                    var scaleMatrix = new THREE.Matrix4(), rotationMatrix = new THREE.Matrix4(), translationMatrix = new THREE.Matrix4(), atom1Vec = new THREE.Vector3(), atom2Vec = new THREE.Vector3(), finalMatrix;
                    var bondsDone = 0;
                    _.forEach(bonds, function (bondObject) {
                        var bond = getBondInfo(bondObject), atom1 = molecule.getAtomById(bond.aId), atom2 = molecule.getAtomById(bond.bId);
                        atom1Vec.fromArray(molecule.getters.getAtomPosition(atom1)), atom2Vec.fromArray(molecule.getters.getAtomPosition(atom2));
                        atomsVector.subVectors(atom1Vec, atom2Vec);
                        center.addVectors(atom1Vec, atom2Vec).divideScalar(2);
                        rotationAxis.crossVectors(atomsVector, up).normalize();
                        rotationAngle = atomsVector.angleTo(up);
                        scaleMatrix.makeScale(1, atomsVector.length(), 1);
                        rotationMatrix.makeRotationAxis(rotationAxis, -rotationAngle);
                        translationMatrix.makeTranslation(center.x, center.y, center.z);
                        finalMatrix = translationMatrix.multiply(rotationMatrix.multiply(scaleMatrix));
                        bondTemplate.applyMatrix(finalMatrix);
                        for (var i = 0; i < bondTemplateVertexBufferLength; i++) {
                            vertexBuffer[bondTemplateVertexBufferLength * bondsDone + i] = bondTemplateVertexBuffer[i];
                            normalBuffer[bondTemplateVertexBufferLength * bondsDone + i] = bondTemplateNormalBuffer[i];
                        }
                        for (var i = 0; i < bondTemplateIndexBufferLength; i++) {
                            indexBuffer[bondTemplateIndexBufferLength * bondsDone + i] = bondTemplateIndexBuffer[i] + bondTemplateVertexCount * bondsDone;
                        }
                        atomsInfo[bond.aId].addBondStart(bondsDone * bondTemplateVertexCount + 1);
                        atomsInfo[bond.bId].addBondStart(bondsDone * bondTemplateVertexCount);
                        translationMatrix.makeTranslation(-center.x, -center.y, -center.z);
                        rotationMatrix.makeRotationAxis(rotationAxis, rotationAngle);
                        scaleMatrix.makeScale(1, 1 / (atomsVector.length()), 1);
                        finalMatrix = scaleMatrix.multiply(rotationMatrix.multiply(translationMatrix));
                        bondTemplate.applyMatrix(finalMatrix);
                        bondsDone += 1;
                    });
                    var atomsDone = 0;
                    _.forEach(atoms, function (atom) {
                        var id = molecule.getters.getAtomId(atom), pos = molecule.getters.getAtomPosition(atom), offset, i, atomInfo = atomsInfo[id];
                        atomInfo.atomStart = bondsDone * bondTemplateVertexBufferLength + atomsDone * atomTemplateVertexBufferLength;
                        atomInfo.atomIndexStart = bondTemplateIndexBufferLength * bondsDone + atomTemplateIndexBufferLength * atomsDone;
                        atomInfo.atomPickStart = atomsDone * atomTemplateVertexBufferLength;
                        cx += pos[0];
                        cy += pos[1];
                        cz += pos[2];
                        translationMatrix.makeTranslation(pos[0], pos[1], pos[2]);
                        finalMatrix = translationMatrix;
                        atomTemplate.applyMatrix(finalMatrix);
                        for (i = 0; i < atomTemplateVertexBufferLength; i++) {
                            offset = bondsDone * bondTemplateVertexBufferLength + atomsDone * atomTemplateVertexBufferLength + i;
                            vertexBuffer[offset] = atomTemplateVertexBuffer[i];
                            normalBuffer[offset] = atomTemplateNormalBuffer[i];
                            offset = atomsDone * atomTemplateVertexBufferLength + i;
                            pickVertexBuffer[offset] = atomTemplateVertexBuffer[i];
                        }
                        for (i = 0; i < atomTemplateVertexCount; i++) {
                            LiteMol.GeometryHelper.setPickColor(pickFlag.value, pickFlag.width, id, pickColorBuffer, atomsDone * atomTemplateVertexCount * 4 + 4 * i);
                        }
                        for (i = 0; i < atomTemplateIndexBufferLength; i++) {
                            offset = bondTemplateIndexBufferLength * bondsDone + atomTemplateIndexBufferLength * atomsDone + i;
                            indexBuffer[offset] = atomTemplateIndexBuffer[i] + (bondTemplateVertexCount) * bondsDone + (atomTemplateVertexCount) * atomsDone;
                            offset = atomTemplateIndexBufferLength * atomsDone + i;
                            pickIndexBuffer[offset] = atomTemplateIndexBuffer[i] + (atomTemplateVertexCount) * atomsDone;
                        }
                        translationMatrix.makeTranslation(-pos[0], -pos[1], -pos[2]);
                        finalMatrix = translationMatrix;
                        atomTemplate.applyMatrix(finalMatrix);
                        atomsDone += 1;
                    });
                    if (atomCount > 0) {
                        cx /= atomCount;
                        cy /= atomCount;
                        cz /= atomCount;
                        _.forEach(atoms, function (atom) {
                            var pos = molecule.getters.getAtomPosition(atom);
                            var dx = cx - pos[0], dy = cy - pos[1], dz = cz - pos[2], mag = dx * dx + dy * dy + dz * dz;
                            if (mag > radiusSq)
                                radiusSq = mag;
                        });
                    }
                    var vb = new THREE.BufferAttribute(vertexBuffer, 3);
                    geometry.addAttribute('position', new THREE.BufferAttribute(vertexBuffer, 3));
                    geometry.addAttribute('normal', new THREE.BufferAttribute(normalBuffer, 3));
                    geometry.addAttribute('index', new THREE.BufferAttribute(indexBuffer, 1));
                    geometry.addAttribute('color', new THREE.BufferAttribute(colorBuffer, 3));
                    pickGeometry.addAttribute('position', new THREE.BufferAttribute(pickVertexBuffer, 3));
                    pickGeometry.addAttribute('index', new THREE.BufferAttribute(pickIndexBuffer, 1));
                    pickGeometry.addAttribute('pColor', new THREE.BufferAttribute(pickColorBuffer, 4));
                    this.geometry = geometry;
                    this.pickGeometry = pickGeometry;
                    this.atomsInfo = atomsInfo;
                    this.atomVertexCount = atomTemplateVertexCount;
                    this.atomTriangleCount = (atomTemplateIndexBufferLength / 3) | 0;
                    this.atomIndexBuffer = atomTemplateIndexBuffer;
                    this.bondVertexCount = bondTemplateVertexCount;
                    this.centroid = new THREE.Vector3(cx, cy, cz);
                    this.radius = Math.sqrt(radiusSq);
                    bondTemplate.dispose();
                    atomTemplate.dispose();
                }
                BallsAndSticksGeometry.prototype.dispose = function () {
                    this.geometry.dispose();
                    this.pickGeometry.dispose();
                };
                BallsAndSticksGeometry.prototype.getBondTemplate = function (radius, tessalation) {
                    var detail;
                    switch (tessalation) {
                        case 0:
                            detail = 4;
                            break;
                        case 1:
                            detail = 6;
                            break;
                        case 2:
                            detail = 8;
                            break;
                        case 3:
                            detail = 10;
                            break;
                        default:
                            detail = 12;
                            break;
                    }
                    var template = LiteMol.GeometryHelper.getIndexedBufferGeometry(new THREE.LatheGeometry([new THREE.Vector3(0, radius, -1 / 2), new THREE.Vector3(0, radius, 1 / 2)], detail, Math.PI));
                    template.applyMatrix(new THREE.Matrix4().makeRotationAxis(new THREE.Vector3(1, 0, 0), -Math.PI / 2));
                    return template;
                };
                BallsAndSticksGeometry.prototype.getAtomTemplate = function (radius, tessalation) {
                    var base;
                    switch (tessalation) {
                        case 0:
                            base = new THREE.OctahedronGeometry(radius, 0);
                            break;
                        case 1:
                            base = new THREE.OctahedronGeometry(radius, 1);
                            break;
                        case 2:
                            base = new THREE.IcosahedronGeometry(radius, 1);
                            break;
                        case 3:
                            base = new THREE.OctahedronGeometry(radius, 2);
                            break;
                        default:
                            base = new THREE.IcosahedronGeometry(radius, 2);
                            break;
                    }
                    base.computeVertexNormals();
                    return LiteMol.GeometryHelper.getIndexedBufferGeometry(base);
                };
                return BallsAndSticksGeometry;
            })(LiteMol.GeometryBase);
            Geometry.BallsAndSticksGeometry = BallsAndSticksGeometry;
            var BallsAndSticksHighlighter = (function (_super) {
                __extends(BallsAndSticksHighlighter, _super);
                function BallsAndSticksHighlighter() {
                    _super.apply(this, arguments);
                }
                BallsAndSticksHighlighter.prototype.highlightInternal = function (params) {
                    var atom = params.atom, vertexCount = params.vertexCount, triangleCount = params.triangleCount, geometry = params.geometry, indexBuffer = params.atomIndexBuffer, buffers = this.buffers;
                    buffers.vertex.array = new Float32Array(geometry.attributes['position'].array.buffer, 4 * atom.atomStart, 3 * vertexCount);
                    buffers.normal.array = new Float32Array(geometry.attributes['normal'].array.buffer, 4 * atom.atomStart, 3 * vertexCount);
                    buffers.index.array = indexBuffer;
                };
                return BallsAndSticksHighlighter;
            })(LiteMol.GeometryHighlighterBase);
            Geometry.BallsAndSticksHighlighter = BallsAndSticksHighlighter;
        })(Geometry = Modes.Geometry || (Modes.Geometry = {}));
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    (function (MoleculeBondType) {
        MoleculeBondType[MoleculeBondType["Unknown"] = 0] = "Unknown";
        MoleculeBondType[MoleculeBondType["Single"] = 1] = "Single";
        MoleculeBondType[MoleculeBondType["Double"] = 2] = "Double";
        MoleculeBondType[MoleculeBondType["Triple"] = 3] = "Triple";
        MoleculeBondType[MoleculeBondType["Aromatic"] = 4] = "Aromatic";
        MoleculeBondType[MoleculeBondType["Metallic"] = 5] = "Metallic";
        MoleculeBondType[MoleculeBondType["Ion"] = 6] = "Ion";
        MoleculeBondType[MoleculeBondType["Hydrogen"] = 7] = "Hydrogen";
        MoleculeBondType[MoleculeBondType["DisulfideBridge"] = 8] = "DisulfideBridge";
    })(LiteMol.MoleculeBondType || (LiteMol.MoleculeBondType = {}));
    var MoleculeBondType = LiteMol.MoleculeBondType;
    (function (SecondaryStructureType) {
        SecondaryStructureType[SecondaryStructureType["Sheet"] = 0] = "Sheet";
        SecondaryStructureType[SecondaryStructureType["Helix"] = 1] = "Helix";
        SecondaryStructureType[SecondaryStructureType["None"] = 2] = "None";
    })(LiteMol.SecondaryStructureType || (LiteMol.SecondaryStructureType = {}));
    var SecondaryStructureType = LiteMol.SecondaryStructureType;
    ;
    var MoleculeHelpers = (function () {
        function MoleculeHelpers() {
        }
        MoleculeHelpers.getRadiusFromSymbol = function (symbol) {
            var radius = MoleculeHelpers.defaultRadii[symbol];
            if (!radius)
                return 1.0;
            return radius;
        };
        MoleculeHelpers.compareResidueIds = function (a, b) {
            if (a.chain === b.chain) {
                if (a.serialNumber === b.serialNumber) {
                    if (a.insertionCode === b.insertionCode)
                        return 0;
                    if (a.insertionCode === undefined)
                        return -1;
                    if (b.insertionCode === undefined)
                        return 1;
                    return a.insertionCode < b.insertionCode ? -1 : 1;
                }
                return a.serialNumber < b.serialNumber ? -1 : 1;
            }
            return a.chain < b.chain ? -1 : 1;
        };
        MoleculeHelpers.defaultRadii = { "H": 1.0, "D": 1.0, "He": 1.75, "Li": 1.82, "Be": 1.53, "B": 0.9, "C": 1.61, "N": 1.55, "O": 1.45, "F": 1.75, "Ne": 1.75, "Na": 2.27, "Mg": 1.73, "Al": 1.84, "Si": 1.8, "P": 1.9, "S": 1.77, "Cl": 1.75, "Ar": 1.75, "K": 2.75, "Ca": 2.31, "Sc": 1.8, "Ti": 1.8, "V": 1.8, "Cr": 1.8, "Mn": 1.0, "Fe": 1.7, "Co": 1.8, "Ni": 1.8, "Cu": 1.8, "Zn": 1.8, "Ga": 1.8, "Ge": 1.75, "As": 1.85, "Se": 1.9, "Br": 1.85, "Kr": 1.75, "Rb": 1.8, "Sr": 2.49, "Y": 1.8, "Zr": 1.8, "Nb": 1.8, "Mo": 1.8, "Tc": 1.8, "Ru": 1.34, "Rh": 1.49, "Pd": 1.8, "Ag": 1.8, "Cd": 1.8, "In": 1.8, "Sn": 1.8, "Sb": 1.75, "Te": 1.0, "I": 1.85, "Xe": 1.75, "Cs": 1.8, "Ba": 1.8, "La": 1.8, "Ce": 1.8, "Pr": 1.8, "Nd": 1.8, "Pm": 1.8, "Sm": 1.8, "Eu": 1.8, "Gd": 1.8, "Tb": 1.8, "Dy": 1.8, "Ho": 1.8, "Er": 1.8, "Tm": 1.8, "Yb": 1.8, "Lu": 1.8, "Hf": 1.59, "Ta": 1.8, "W": 1.39, "Re": 1.8, "Os": 1.8, "Ir": 1.47, "Pt": 1.0, "Au": 1.8, "Hg": 1.0, "Tl": 1.8, "Pb": 1.8, "Bi": 1.8, "Po": 1.75, "At": 1.75, "Rn": 1.75, "Fr": 1.8, "Ra": 1.8, "Ac": 1.8, "Th": 1.8, "Pa": 1.8, "U": 1.8, "Np": 1.8, "Pu": 1.8, "Am": 1.8, "Cm": 1.8, "Bk": 1.8, "Cf": 1.8, "Es": 1.8, "Fm": 1.8, "Md": 1.8, "No": 1.8, "Lr": 1.8, "Rf": 1.8, "Db": 1.8, "Sg": 1.8, "Bh": 1.8, "Hs": 1.8, "Mt": 1.8 };
        return MoleculeHelpers;
    })();
    LiteMol.MoleculeHelpers = MoleculeHelpers;
    var Molecule = (function () {
        function Molecule(data, getters) {
            this.data = data;
            this.getters = getters;
        }
        Molecule.prototype.getAtoms = function () {
            return this.getters.getAtoms(this.data);
        };
        Molecule.prototype.getBonds = function () {
            return this.getters.getBonds(this.data);
        };
        Molecule.prototype.getAtomById = function (id) {
            return this.getters.getAtomById(this.data, id);
        };
        return Molecule;
    })();
    LiteMol.Molecule = Molecule;
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Utils;
    (function (Utils) {
        "use strict";
        var IsoSurfaceVertex = (function () {
            function IsoSurfaceVertex(id, x, y, z, annotation) {
                this.id = id;
                this.x = +x;
                this.y = +y;
                this.z = +z;
                this.nx = 0.0;
                this.ny = 0.0;
                this.nz = 0.0;
                this.annotation = annotation | 0;
            }
            return IsoSurfaceVertex;
        })();
        Utils.IsoSurfaceVertex = IsoSurfaceVertex;
        var MarchingCubesResult = (function () {
            function MarchingCubesResult(vertices, triangleIndices) {
                this.vertices = vertices;
                this.triangleIndices = triangleIndices;
            }
            return MarchingCubesResult;
        })();
        Utils.MarchingCubesResult = MarchingCubesResult;
        var MarchingCubes = (function () {
            function MarchingCubes() {
            }
            MarchingCubes.compute = function (params) {
                var state = new MarchingCubesState(params), nX = params.dimenstions[0], nY = params.dimenstions[1], nZ = params.dimenstions[2], i, j, k;
                for (i = 0; i < nX - 1; i++) {
                    for (j = 0; j < nY - 1; j++) {
                        for (k = 0; k < nZ - 1; k++) {
                            state.processCell(i, j, k);
                        }
                    }
                }
                state.scalarField = null;
                state.annotationField = null;
                state.vertices = null;
                state.calculateNormals();
                return new MarchingCubesResult(state.vertexArray, state.triangles);
            };
            return MarchingCubes;
        })();
        Utils.MarchingCubes = MarchingCubes;
        var Index = (function () {
            function Index(i, j, k) {
                this.i = i | 0;
                this.j = j | 0;
                this.k = k | 0;
            }
            return Index;
        })();
        var IndexPair = (function () {
            function IndexPair(a, b) {
                this.a = a;
                this.b = b;
            }
            return IndexPair;
        })();
        var MarchingCubesState = (function () {
            function MarchingCubesState(params) {
                this.vertices = {};
                this.edgeId = 0;
                this.vertexId = 0;
                this.vertList = new Array(12);
                this.i = 0;
                this.j = 0;
                this.k = 0;
                this.triangles = [];
                this.vertexArray = [];
                this.nX = params.dimenstions[0], this.nY = params.dimenstions[1], this.nZ = params.dimenstions[2], this.minX = params.bottomLeft[0], this.minY = params.bottomLeft[1], this.minZ = params.bottomLeft[2], this.dX = params.deltas[0], this.dY = params.deltas[1], this.dZ = params.deltas[2], this.isoLevel = params.isoLevel, this.scalarField = params.scalarField, this.annotationField = params.annotationField || [];
                for (var i = 0; i < 12; i++) {
                    this.vertList[i] = 0;
                }
            }
            MarchingCubesState.prototype.getFieldFromIndices = function (i, j, k) {
                return this.scalarField[this.nZ * (i * this.nY + j) + k];
            };
            MarchingCubesState.prototype.get3dOffsetFromEdgeInfo = function (index) {
                return this.nZ * ((this.i + index.i) * this.nY + this.j + index.j) + this.k + index.k;
            };
            MarchingCubesState.prototype.setEdgeInfo = function (nEdgeNo) {
                var info = MarchingCubesState.edgeIdInfo[nEdgeNo], vId = this.get3dOffsetFromEdgeInfo(info);
                this.edgeId = 3 * vId + info.e;
                this.vertexId = vId;
            };
            MarchingCubesState.prototype.interpolate = function (edgeNum) {
                this.setEdgeInfo(edgeNum);
                var ret = this.vertices[this.edgeId];
                if (ret)
                    return ret.id;
                var edge = MarchingCubesState.cubeEdges[edgeNum];
                var a = edge.a, b = edge.b, li = a.i + this.i, lj = a.j + this.j, lk = a.k + this.k, hi = b.i + this.i, hj = b.j + this.j, hk = b.k + this.k, v0 = this.getFieldFromIndices(li, lj, lk), v1 = this.getFieldFromIndices(hi, hj, hk), t = (this.isoLevel - v0) / (v0 - v1), id = this.vertexArray.length;
                ret = new IsoSurfaceVertex(id, this.minX + this.dX * (li + t * (li - hi)), this.minY + this.dY * (lj + t * (lj - hj)), this.minZ + this.dZ * (lk + t * (lk - hk)), this.annotationField[this.vertexId]);
                this.vertices[this.edgeId] = ret;
                this.vertexArray[id] = ret;
                return id;
            };
            MarchingCubesState.prototype.processCell = function (i, j, k) {
                var tableIndex = 0, t, triInfo, triCount = this.triangles.length, vertList = this.vertList;
                if (this.getFieldFromIndices(i, j, k) < this.isoLevel)
                    tableIndex |= 1;
                if (this.getFieldFromIndices(i + 1, j, k) < this.isoLevel)
                    tableIndex |= 2;
                if (this.getFieldFromIndices(i + 1, j + 1, k) < this.isoLevel)
                    tableIndex |= 4;
                if (this.getFieldFromIndices(i, j + 1, k) < this.isoLevel)
                    tableIndex |= 8;
                if (this.getFieldFromIndices(i, j, k + 1) < this.isoLevel)
                    tableIndex |= 16;
                if (this.getFieldFromIndices(i + 1, j, k + 1) < this.isoLevel)
                    tableIndex |= 32;
                if (this.getFieldFromIndices(i + 1, j + 1, k + 1) < this.isoLevel)
                    tableIndex |= 64;
                if (this.getFieldFromIndices(i, j + 1, k + 1) < this.isoLevel)
                    tableIndex |= 128;
                this.i = i;
                this.j = j;
                this.k = k;
                var edgeInfo = MarchingCubesState.edgeTable[tableIndex];
                if ((edgeInfo & 1) > 0)
                    vertList[0] = this.interpolate(0);
                if ((edgeInfo & 2) > 0)
                    vertList[1] = this.interpolate(1);
                if ((edgeInfo & 4) > 0)
                    vertList[2] = this.interpolate(2);
                if ((edgeInfo & 8) > 0)
                    vertList[3] = this.interpolate(3);
                if ((edgeInfo & 16) > 0)
                    vertList[4] = this.interpolate(4);
                if ((edgeInfo & 32) > 0)
                    vertList[5] = this.interpolate(5);
                if ((edgeInfo & 64) > 0)
                    vertList[6] = this.interpolate(6);
                if ((edgeInfo & 128) > 0)
                    vertList[7] = this.interpolate(7);
                if ((edgeInfo & 256) > 0)
                    vertList[8] = this.interpolate(8);
                if ((edgeInfo & 512) > 0)
                    vertList[9] = this.interpolate(9);
                if ((edgeInfo & 1024) > 0)
                    vertList[10] = this.interpolate(10);
                if ((edgeInfo & 2048) > 0)
                    vertList[11] = this.interpolate(11);
                triInfo = MarchingCubesState.triTable[tableIndex];
                for (t = 0; triInfo[t] !== -1; t += 3) {
                    this.triangles[triCount++] = vertList[triInfo[t]];
                    this.triangles[triCount++] = vertList[triInfo[t + 1]];
                    this.triangles[triCount++] = vertList[triInfo[t + 2]];
                }
            };
            MarchingCubesState.prototype.calculateNormals = function () {
                var i, len = this.triangles.length, tri = this.triangles, vertexArray = this.vertexArray, a, b, c, nx, ny, nz, f;
                for (i = 0; i < len; i += 3) {
                    a = vertexArray[tri[i]];
                    b = vertexArray[tri[i + 1]];
                    c = vertexArray[tri[i + 2]];
                    nx = a.z * (b.y - c.y) + b.z * c.y - b.y * c.z + a.y * (-b.z + c.z);
                    ny = -(b.z * c.x) + a.z * (-b.x + c.x) + a.x * (b.z - c.z) + b.x * c.z;
                    nz = a.y * (b.x - c.x) + b.y * c.x - b.x * c.y + a.x * (-b.y + c.y);
                    a.nx += nx;
                    a.ny += ny;
                    a.nz += nz;
                    b.nx += nx;
                    b.ny += ny;
                    b.nz += nz;
                    c.nx += nx;
                    c.ny += ny;
                    c.nz += nz;
                }
                len = vertexArray.length;
                for (i = 0; i < len; i++) {
                    a = vertexArray[i];
                    f = -1.0 / Math.sqrt(a.nx * a.nx + a.ny * a.ny + a.nz * a.nz);
                    a.nx *= f;
                    a.ny *= f;
                    a.nz *= f;
                }
            };
            MarchingCubesState.cubeVertices = [
                new Index(0, 0, 0),
                new Index(1, 0, 0),
                new Index(1, 1, 0),
                new Index(0, 1, 0),
                new Index(0, 0, 1),
                new Index(1, 0, 1),
                new Index(1, 1, 1),
                new Index(0, 1, 1),
            ];
            MarchingCubesState.cubeEdges = [
                new IndexPair(MarchingCubesState.cubeVertices[0], MarchingCubesState.cubeVertices[1]),
                new IndexPair(MarchingCubesState.cubeVertices[1], MarchingCubesState.cubeVertices[2]),
                new IndexPair(MarchingCubesState.cubeVertices[2], MarchingCubesState.cubeVertices[3]),
                new IndexPair(MarchingCubesState.cubeVertices[3], MarchingCubesState.cubeVertices[0]),
                new IndexPair(MarchingCubesState.cubeVertices[4], MarchingCubesState.cubeVertices[5]),
                new IndexPair(MarchingCubesState.cubeVertices[5], MarchingCubesState.cubeVertices[6]),
                new IndexPair(MarchingCubesState.cubeVertices[6], MarchingCubesState.cubeVertices[7]),
                new IndexPair(MarchingCubesState.cubeVertices[7], MarchingCubesState.cubeVertices[4]),
                new IndexPair(MarchingCubesState.cubeVertices[0], MarchingCubesState.cubeVertices[4]),
                new IndexPair(MarchingCubesState.cubeVertices[1], MarchingCubesState.cubeVertices[5]),
                new IndexPair(MarchingCubesState.cubeVertices[2], MarchingCubesState.cubeVertices[6]),
                new IndexPair(MarchingCubesState.cubeVertices[3], MarchingCubesState.cubeVertices[7]),
            ];
            MarchingCubesState.edgeTable = new Int32Array([
                0x0,
                0x109,
                0x203,
                0x30a,
                0x406,
                0x50f,
                0x605,
                0x70c,
                0x80c,
                0x905,
                0xa0f,
                0xb06,
                0xc0a,
                0xd03,
                0xe09,
                0xf00,
                0x190,
                0x99,
                0x393,
                0x29a,
                0x596,
                0x49f,
                0x795,
                0x69c,
                0x99c,
                0x895,
                0xb9f,
                0xa96,
                0xd9a,
                0xc93,
                0xf99,
                0xe90,
                0x230,
                0x339,
                0x33,
                0x13a,
                0x636,
                0x73f,
                0x435,
                0x53c,
                0xa3c,
                0xb35,
                0x83f,
                0x936,
                0xe3a,
                0xf33,
                0xc39,
                0xd30,
                0x3a0,
                0x2a9,
                0x1a3,
                0xaa,
                0x7a6,
                0x6af,
                0x5a5,
                0x4ac,
                0xbac,
                0xaa5,
                0x9af,
                0x8a6,
                0xfaa,
                0xea3,
                0xda9,
                0xca0,
                0x460,
                0x569,
                0x663,
                0x76a,
                0x66,
                0x16f,
                0x265,
                0x36c,
                0xc6c,
                0xd65,
                0xe6f,
                0xf66,
                0x86a,
                0x963,
                0xa69,
                0xb60,
                0x5f0,
                0x4f9,
                0x7f3,
                0x6fa,
                0x1f6,
                0xff,
                0x3f5,
                0x2fc,
                0xdfc,
                0xcf5,
                0xfff,
                0xef6,
                0x9fa,
                0x8f3,
                0xbf9,
                0xaf0,
                0x650,
                0x759,
                0x453,
                0x55a,
                0x256,
                0x35f,
                0x55,
                0x15c,
                0xe5c,
                0xf55,
                0xc5f,
                0xd56,
                0xa5a,
                0xb53,
                0x859,
                0x950,
                0x7c0,
                0x6c9,
                0x5c3,
                0x4ca,
                0x3c6,
                0x2cf,
                0x1c5,
                0xcc,
                0xfcc,
                0xec5,
                0xdcf,
                0xcc6,
                0xbca,
                0xac3,
                0x9c9,
                0x8c0,
                0x8c0,
                0x9c9,
                0xac3,
                0xbca,
                0xcc6,
                0xdcf,
                0xec5,
                0xfcc,
                0xcc,
                0x1c5,
                0x2cf,
                0x3c6,
                0x4ca,
                0x5c3,
                0x6c9,
                0x7c0,
                0x950,
                0x859,
                0xb53,
                0xa5a,
                0xd56,
                0xc5f,
                0xf55,
                0xe5c,
                0x15c,
                0x55,
                0x35f,
                0x256,
                0x55a,
                0x453,
                0x759,
                0x650,
                0xaf0,
                0xbf9,
                0x8f3,
                0x9fa,
                0xef6,
                0xfff,
                0xcf5,
                0xdfc,
                0x2fc,
                0x3f5,
                0xff,
                0x1f6,
                0x6fa,
                0x7f3,
                0x4f9,
                0x5f0,
                0xb60,
                0xa69,
                0x963,
                0x86a,
                0xf66,
                0xe6f,
                0xd65,
                0xc6c,
                0x36c,
                0x265,
                0x16f,
                0x66,
                0x76a,
                0x663,
                0x569,
                0x460,
                0xca0,
                0xda9,
                0xea3,
                0xfaa,
                0x8a6,
                0x9af,
                0xaa5,
                0xbac,
                0x4ac,
                0x5a5,
                0x6af,
                0x7a6,
                0xaa,
                0x1a3,
                0x2a9,
                0x3a0,
                0xd30,
                0xc39,
                0xf33,
                0xe3a,
                0x936,
                0x83f,
                0xb35,
                0xa3c,
                0x53c,
                0x435,
                0x73f,
                0x636,
                0x13a,
                0x33,
                0x339,
                0x230,
                0xe90,
                0xf99,
                0xc93,
                0xd9a,
                0xa96,
                0xb9f,
                0x895,
                0x99c,
                0x69c,
                0x795,
                0x49f,
                0x596,
                0x29a,
                0x393,
                0x99,
                0x190,
                0xf00,
                0xe09,
                0xd03,
                0xc0a,
                0xb06,
                0xa0f,
                0x905,
                0x80c,
                0x70c,
                0x605,
                0x50f,
                0x406,
                0x30a,
                0x203,
                0x109,
                0x0
            ]);
            MarchingCubesState.triTable = [
                ([-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1]),
                ([8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1]),
                ([3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1]),
                ([4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1]),
                ([4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1]),
                ([9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1]),
                ([10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1]),
                ([5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1]),
                ([5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1]),
                ([8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1]),
                ([2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1]),
                ([2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1]),
                ([11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1]),
                ([5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1]),
                ([11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1]),
                ([11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1]),
                ([2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1]),
                ([6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1]),
                ([3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1]),
                ([6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1]),
                ([6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1]),
                ([8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1]),
                ([7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1]),
                ([3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1]),
                ([0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1]),
                ([9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1]),
                ([8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1]),
                ([5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1]),
                ([0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1]),
                ([6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1]),
                ([10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1]),
                ([1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1]),
                ([0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1]),
                ([3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1]),
                ([6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1]),
                ([9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1]),
                ([8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1]),
                ([3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1]),
                ([10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1]),
                ([10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1]),
                ([2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1]),
                ([7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1]),
                ([2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1]),
                ([1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1]),
                ([11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1]),
                ([8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1]),
                ([0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1]),
                ([7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1]),
                ([7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1]),
                ([10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1]),
                ([0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1]),
                ([7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1]),
                ([6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1]),
                ([4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1]),
                ([10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1]),
                ([8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1]),
                ([1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1]),
                ([10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1]),
                ([10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1]),
                ([9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1]),
                ([7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1]),
                ([3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1]),
                ([7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1]),
                ([3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1]),
                ([6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1]),
                ([9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1]),
                ([1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1]),
                ([4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1]),
                ([7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1]),
                ([6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1]),
                ([0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1]),
                ([6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1]),
                ([0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1]),
                ([11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1]),
                ([6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1]),
                ([5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1]),
                ([9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1]),
                ([1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1]),
                ([10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1]),
                ([0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1]),
                ([11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1]),
                ([9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1]),
                ([7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1]),
                ([2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1]),
                ([9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1]),
                ([9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1]),
                ([1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1]),
                ([0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1]),
                ([10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1]),
                ([2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1]),
                ([0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1]),
                ([0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1]),
                ([9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1]),
                ([5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1]),
                ([5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1]),
                ([8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1]),
                ([9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1]),
                ([1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1]),
                ([3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1]),
                ([4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1]),
                ([9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1]),
                ([11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1]),
                ([11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1]),
                ([2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1]),
                ([9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1]),
                ([3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1]),
                ([1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1]),
                ([4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1]),
                ([0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1]),
                ([9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1]),
                ([1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
                ([-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1])
            ];
            MarchingCubesState.edgeIdInfo = [
                { i: 0, j: 0, k: 0, e: 0 },
                { i: 1, j: 0, k: 0, e: 1 },
                { i: 0, j: 1, k: 0, e: 0 },
                { i: 0, j: 0, k: 0, e: 1 },
                { i: 0, j: 0, k: 1, e: 0 },
                { i: 1, j: 0, k: 1, e: 1 },
                { i: 0, j: 1, k: 1, e: 0 },
                { i: 0, j: 0, k: 1, e: 1 },
                { i: 0, j: 0, k: 0, e: 2 },
                { i: 1, j: 0, k: 0, e: 2 },
                { i: 1, j: 1, k: 0, e: 2 },
                { i: 0, j: 1, k: 0, e: 2 }
            ];
            return MarchingCubesState;
        })();
    })(Utils = LiteMol.Utils || (LiteMol.Utils = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        var Geometry;
        (function (Geometry) {
            var MolecularSurfaceGeometryAtomVertexInfo = (function () {
                function MolecularSurfaceGeometryAtomVertexInfo() {
                    this.vertices = [];
                    this.triangleIndices = [];
                    this.vertexCount = 0;
                    this.triangleIndexCount = 0;
                    this.vertices.length = 4;
                    this.triangleIndexCount = 12;
                }
                MolecularSurfaceGeometryAtomVertexInfo.prototype.addVertex = function (v) {
                    this.vertices[this.vertexCount++] = v;
                };
                MolecularSurfaceGeometryAtomVertexInfo.prototype.addTriangle = function (a, b, c) {
                    var i = this.triangleIndexCount;
                    this.triangleIndices[i++] = a;
                    this.triangleIndices[i++] = b;
                    this.triangleIndices[i++] = c;
                    this.triangleIndexCount = i;
                };
                return MolecularSurfaceGeometryAtomVertexInfo;
            })();
            Geometry.MolecularSurfaceGeometryAtomVertexInfo = MolecularSurfaceGeometryAtomVertexInfo;
            var MolecularSurfaceGeometry = (function (_super) {
                __extends(MolecularSurfaceGeometry, _super);
                function MolecularSurfaceGeometry(molecule, probeRadius, density) {
                    _super.call(this);
                    var data = {
                        atoms: molecule.getAtoms(),
                        getAtomPosition: molecule.getters.getAtomPosition,
                        getAtomId: molecule.getters.getAtomId,
                        getVdwRadius: molecule.getters.getAtomRadius
                    };
                    var surface = MolecularSurfaceGeometry.createSurface(molecule, probeRadius, density);
                    var atomsInfo = {}, getAtomId = molecule.getters.getAtomId;
                    _.forEach(data.atoms, function (atom) {
                        atomsInfo[getAtomId(atom)] = new MolecularSurfaceGeometryAtomVertexInfo();
                    });
                    var geometry = new THREE.BufferGeometry(), pickGeometry = new THREE.BufferGeometry(), pickExcludeGeometry = new THREE.BufferGeometry(), exludedTriangleCount = 0, a, b, c, cx = 0.0, cy = 0.0, cz = 0.0, radiusSq = 0.0, i, triangleIndexCount = surface.triangleIndices.length, vertices = surface.vertices, triangleIndices = surface.triangleIndices;
                    var bufferSize = vertices.length * 3, vertexBuffer = new Float32Array(bufferSize), normalBuffer = new Float32Array(bufferSize), colorBuffer = new Float32Array(bufferSize), indexBuffer = new Uint32Array(triangleIndices), pickIndexBuffer, pickExcludeIndexBuffer, pickBuffer = new Float32Array(((triangleIndices.length / 3) * 4) | 0), pickColor = new THREE.Color();
                    _.forEach(surface.vertices, function (v) {
                        var offset = 3 * v.id;
                        vertexBuffer[offset] = v.x;
                        vertexBuffer[offset + 1] = v.y;
                        vertexBuffer[offset + 2] = v.z;
                        cx += v.x;
                        cy += v.y;
                        cz += v.z;
                        normalBuffer[offset] = v.nx;
                        normalBuffer[offset + 1] = v.ny;
                        normalBuffer[offset + 2] = v.nz;
                        LiteMol.GeometryHelper.setPickColor(0, 0, v.annotation, pickBuffer, 4 * v.id);
                        var info = atomsInfo[v.annotation];
                        if (info)
                            info.addVertex(v.id);
                    });
                    if (surface.vertices.length > 0) {
                        cx /= surface.vertices.length;
                        cy /= surface.vertices.length;
                        cz /= surface.vertices.length;
                        _.forEach(surface.vertices, function (v) {
                            var dx = cx - v.x, dy = cy - v.y, dz = cz - v.z, mag = dx * dx + dy * dy + dz * dz;
                            if (mag > radiusSq)
                                radiusSq = mag;
                        });
                    }
                    var exludedTriangleCount = 0;
                    for (i = 0; i < triangleIndexCount; i += 3) {
                        a = vertices[triangleIndices[i]];
                        b = vertices[triangleIndices[i + 1]];
                        c = vertices[triangleIndices[i + 2]];
                        if (a.annotation !== b.annotation || b.annotation !== c.annotation || a.annotation !== c.annotation) {
                            exludedTriangleCount++;
                        }
                        else {
                            atomsInfo[a.annotation].addTriangle(a.id, b.id, c.id);
                        }
                    }
                    pickIndexBuffer = new Uint32Array((surface.triangleIndices.length - exludedTriangleCount) * 3);
                    pickExcludeIndexBuffer = new Uint32Array(exludedTriangleCount * 3);
                    var pickIndexOffset = 0, pickExludeIndexOffset = 0;
                    for (i = 0; i < triangleIndexCount; i += 3) {
                        a = vertices[triangleIndices[i]];
                        b = vertices[triangleIndices[i + 1]];
                        c = vertices[triangleIndices[i + 2]];
                        if (a.annotation !== b.annotation || b.annotation !== c.annotation || a.annotation !== c.annotation) {
                            pickExcludeIndexBuffer[3 * pickExludeIndexOffset + 0] = a.id;
                            pickExcludeIndexBuffer[3 * pickExludeIndexOffset + 1] = b.id;
                            pickExcludeIndexBuffer[3 * pickExludeIndexOffset + 2] = c.id;
                            pickExludeIndexOffset++;
                        }
                        else {
                            pickIndexBuffer[3 * pickIndexOffset + 0] = a.id;
                            pickIndexBuffer[3 * pickIndexOffset + 1] = b.id;
                            pickIndexBuffer[3 * pickIndexOffset + 2] = c.id;
                            pickIndexOffset++;
                        }
                    }
                    geometry.addAttribute('position', new THREE.BufferAttribute(vertexBuffer, 3));
                    geometry.addAttribute('normal', new THREE.BufferAttribute(normalBuffer, 3));
                    geometry.addAttribute('index', new THREE.BufferAttribute(indexBuffer, 1));
                    geometry.addAttribute('color', new THREE.BufferAttribute(colorBuffer, 3));
                    var vb = new THREE.BufferAttribute(vertexBuffer, 3);
                    pickGeometry.addAttribute('position', vb);
                    pickGeometry.addAttribute('index', new THREE.BufferAttribute(pickIndexBuffer, 1));
                    pickGeometry.addAttribute('pColor', new THREE.BufferAttribute(pickBuffer, 4));
                    pickExcludeGeometry.addAttribute('position', vb);
                    pickExcludeGeometry.addAttribute('index', new THREE.BufferAttribute(pickExcludeIndexBuffer, 1));
                    this.geometry = geometry;
                    this.pickGeometry = pickGeometry;
                    this.pickExcludeGeometry = pickExcludeGeometry;
                    this.atomsInfo = atomsInfo;
                    this.centroid = new THREE.Vector3(cx, cy, cz);
                    this.radius = Math.sqrt(radiusSq);
                }
                MolecularSurfaceGeometry.prototype.dispose = function () {
                    this.geometry.dispose();
                    this.pickGeometry.dispose();
                    this.pickExcludeGeometry.dispose();
                };
                MolecularSurfaceGeometry.createSurface = function (molecule, probeRadius, density) {
                    var atoms = molecule.getAtoms(), getters = molecule.getters;
                    function radius(atom) {
                        return getters.getAtomRadius(atom) + 2 * probeRadius;
                    }
                    var minX = Number.MAX_VALUE, minY = Number.MAX_VALUE, minZ = Number.MAX_VALUE, maxX = Number.MIN_VALUE, maxY = Number.MIN_VALUE, maxZ = Number.MIN_VALUE;
                    _.forEach(atoms, function (a) {
                        var r = radius(a);
                        var position = getters.getAtomPosition(a);
                        minX = Math.min(minX, position[0] - r);
                        minY = Math.min(minY, position[1] - r);
                        minZ = Math.min(minZ, position[2] - r);
                        maxX = Math.max(maxX, position[0] + r);
                        maxY = Math.max(maxY, position[1] + r);
                        maxZ = Math.max(maxZ, position[2] + r);
                    });
                    minX -= 1.5;
                    minY -= 1.5;
                    minZ -= 1.5;
                    maxX += 1.5;
                    maxY += 1.5;
                    maxZ += 1.5;
                    var nX = Math.floor((maxX - minX) * density), nY = Math.floor((maxY - minY) * density), nZ = Math.floor((maxZ - minZ) * density);
                    nX = Math.min(nX, 333);
                    nY = Math.min(nY, 333);
                    nZ = Math.min(nZ, 333);
                    var dX = (maxX - minX) / (nX - 1), dY = (maxY - minY) / (nY - 1), dZ = (maxZ - minZ) / (nZ - 1), dMax = Math.max(dX, dY, dZ);
                    var i, len = nX * nY * nZ, field = new Float32Array(len), maxField = new Float32Array(len), proximityMap = new Int32Array(len);
                    for (i = 0; i < len; i++) {
                        proximityMap[i] = -1.0;
                    }
                    function getMinIndex(x, y, z) {
                        return {
                            i: Math.max(Math.floor((x - minX) / dX) - 1, 0),
                            j: Math.max(Math.floor((y - minY) / dY) - 1, 0),
                            k: Math.max(Math.floor((z - minZ) / dZ) - 1, 0)
                        };
                    }
                    function getMaxIndex(x, y, z) {
                        return {
                            i: Math.min(Math.ceil((x - minX) / dX) + 1, nX),
                            j: Math.min(Math.ceil((y - minY) / dY) + 1, nY),
                            k: Math.min(Math.ceil((z - minZ) / dZ) + 1, nZ)
                        };
                    }
                    function addBall(id, center, strength) {
                        var i, j, k, x, xx, y, yy, z, zz, v, offset, xo, yo, maxRsq = strength * strength, lower = getMinIndex(center[0] - strength, center[1] - strength, center[2] - strength), upper = getMaxIndex(center[0] + strength, center[1] + strength, center[2] + strength), cx = center[0], cy = center[1], cz = center[2], mini = lower.i, minj = lower.j, mink = lower.k, maxi = upper.i, maxj = upper.j, maxk = upper.k;
                        for (i = mini; i < maxi; i++) {
                            x = minX + i * dX - cx;
                            xx = x * x;
                            xo = i * nY;
                            for (j = minj; j < maxj; j++) {
                                y = minY + j * dY - cy;
                                yy = xx + y * y;
                                yo = nZ * (xo + j);
                                for (k = mink; k < maxk; k++) {
                                    offset = yo + k;
                                    z = minZ + k * dZ - cz;
                                    zz = yy + z * z;
                                    if (zz >= maxRsq)
                                        continue;
                                    v = strength / Math.sqrt(0.000001 + zz) - 1;
                                    if (v > 0) {
                                        field[offset] += v;
                                        if (v > maxField[offset]) {
                                            proximityMap[offset] = id;
                                            maxField[offset] = v;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    _.forEach(atoms, function (a) {
                        addBall(getters.getAtomId(a), getters.getAtomPosition(a), radius(a));
                    });
                    maxField = null;
                    var params = {
                        dimenstions: [nX, nY, nZ],
                        bottomLeft: [minX, minY, minZ],
                        deltas: [dX, dY, dZ],
                        isoLevel: 1,
                        scalarField: field,
                        annotationField: proximityMap
                    };
                    var ret = LiteMol.Utils.MarchingCubes.compute(params);
                    field = null;
                    proximityMap = null;
                    params = null;
                    return ret;
                };
                return MolecularSurfaceGeometry;
            })(LiteMol.GeometryBase);
            Geometry.MolecularSurfaceGeometry = MolecularSurfaceGeometry;
            var MolecularSurfaceHighlighter = (function (_super) {
                __extends(MolecularSurfaceHighlighter, _super);
                function MolecularSurfaceHighlighter() {
                    _super.apply(this, arguments);
                }
                MolecularSurfaceHighlighter.prototype.highlightInternal = function (params) {
                    var atom = params.atom, vertices = atom.vertices, vertexCount = vertices.length, triangles = atom.triangleIndices, triangleIndexCount = triangles.length, geometry = params.geometry, buffers = this.buffers, vertexMap = {}, i, v;
                    this.bufferSize = vertexCount;
                    this.indexBufferSize = triangleIndexCount;
                    this.ensureBuffersSize();
                    var vb = this.arrays.vertex, nb = this.arrays.normal, ib = this.arrays.index, vbSrc = geometry.attributes['position'].array, nbSrc = geometry.attributes['normal'].array;
                    for (i = 0; i < vertexCount; i = (i + 1) | 0) {
                        v = vertices[i];
                        vb[3 * i] = vbSrc[3 * v];
                        vb[3 * i + 1] = vbSrc[3 * v + 1];
                        vb[3 * i + 2] = vbSrc[3 * v + 2];
                        nb[3 * i] = nbSrc[3 * v];
                        nb[3 * i + 1] = nbSrc[3 * v + 1];
                        nb[3 * i + 2] = nbSrc[3 * v + 2];
                        vertexMap[v] = i;
                    }
                    for (i = 0; i < triangleIndexCount; i = (i + 1) | 0) {
                        v = vertexMap[triangles[i]];
                        ib[i] = v;
                    }
                    buffers.vertex.array = new Float32Array(vb.buffer, 0, 3 * vertexCount);
                    buffers.normal.array = new Float32Array(nb.buffer, 0, 3 * vertexCount);
                    buffers.index.array = new Uint32Array(ib.buffer, 0, triangleIndexCount);
                };
                return MolecularSurfaceHighlighter;
            })(LiteMol.GeometryHighlighterBase);
            Geometry.MolecularSurfaceHighlighter = MolecularSurfaceHighlighter;
        })(Geometry = Modes.Geometry || (Modes.Geometry = {}));
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        (function (BallsAndSticksDrawingStyle) {
            BallsAndSticksDrawingStyle[BallsAndSticksDrawingStyle["BallsAndSticks"] = 0] = "BallsAndSticks";
            BallsAndSticksDrawingStyle[BallsAndSticksDrawingStyle["VdwSpheres"] = 1] = "VdwSpheres";
        })(Modes.BallsAndSticksDrawingStyle || (Modes.BallsAndSticksDrawingStyle = {}));
        var BallsAndSticksDrawingStyle = Modes.BallsAndSticksDrawingStyle;
        var BallsAndSticksDrawing = (function (_super) {
            __extends(BallsAndSticksDrawing, _super);
            function BallsAndSticksDrawing() {
                _super.apply(this, arguments);
                this.highlighter = new Modes.Geometry.BallsAndSticksHighlighter();
            }
            BallsAndSticksDrawing.prototype.addedToSceneInternal = function () {
                var buffer = this.ballsAndSticks.pickGeometry.attributes['pColor'].array, len = Math.floor(buffer.length / 4);
                for (var i = 0; i < len; i++) {
                    buffer[4 * i + 3] = this.sceneId / 255.0;
                }
                this.ballsAndSticks.pickGeometry.attributes['pColor'].needsUpdate = true;
            };
            BallsAndSticksDrawing.prototype.getPickElement = function (pickId) {
                var el = this.molecule.getAtomById(pickId);
                if (el) {
                    return { object: this.molecule, element: el };
                }
                return null;
            };
            BallsAndSticksDrawing.prototype.highlightElement = function (pickId) {
                if (this.highlighter.isElementHighlighted(pickId))
                    return false;
                var atom = this.ballsAndSticks.atomsInfo[pickId];
                if (atom) {
                    return this.highlighter.highlight(pickId, this.theme.highlightColor, {
                        atom: atom,
                        vertexCount: this.ballsAndSticks.atomVertexCount,
                        atomIndexBuffer: this.ballsAndSticks.atomIndexBuffer,
                        triangleCount: this.ballsAndSticks.atomTriangleCount,
                        geometry: this.ballsAndSticks.geometry
                    });
                }
                return this.highlighter.clear();
            };
            BallsAndSticksDrawing.prototype.clearHighlight = function () {
                return this.highlighter.clear();
            };
            BallsAndSticksDrawing.prototype.applyTheme = function (theme) {
                var _this = this;
                _super.prototype.applyTheme.call(this, theme);
                var colorBuffer = this.ballsAndSticks.geometry.attributes['color'].array, positionBuffer = this.ballsAndSticks.geometry.attributes['position'].array, pickPositionBuffer = this.ballsAndSticks.pickGeometry.attributes['position'].array, map = this.ballsAndSticks.atomsInfo, color = { r: 0.1, g: 0.1, b: 0.1 }, getAtomPosition = this.molecule.getters.getAtomPosition, getAtomId = this.molecule.getters.getAtomId, getAtomRadius = this.molecule.getters.getAtomRadius;
                _.forEach(this.molecule.getAtoms(), function (a) {
                    var info = map[getAtomId(a)], start = info.atomStart, pickStart = info.atomPickStart, end = info.atomStart + 3 * _this.ballsAndSticks.atomVertexCount, bondLen = _this.ballsAndSticks.bondVertexCount, i, x, y, z, center = getAtomPosition(a), cx = center[0], cy = center[1], cz = center[2], scale = _this.ballsAndSticks.isVdw ? getAtomRadius(a) : theme.getAtomScale(_this.molecule, a), factor = scale / info.scale;
                    theme.setAtomColor(_this.molecule, a, color);
                    info.scale = scale;
                    for (i = start; i < end; i += 3) {
                        colorBuffer[i] = color.r;
                        colorBuffer[i + 1] = color.g;
                        colorBuffer[i + 2] = color.b;
                    }
                    if (Math.abs(factor - 1) > 0.001) {
                        for (i = start; i < end; i += 3) {
                            x = positionBuffer[i] = factor * (positionBuffer[i] - cx) + cx;
                            y = positionBuffer[i + 1] = factor * (positionBuffer[i + 1] - cy) + cy;
                            z = positionBuffer[i + 2] = factor * (positionBuffer[i + 2] - cz) + cz;
                            pickPositionBuffer[i - start + pickStart] = x;
                            pickPositionBuffer[i - start + pickStart + 1] = y;
                            pickPositionBuffer[i - start + pickStart + 2] = z;
                        }
                    }
                    _.forEach(info.bondStarts, function (bs) {
                        end = bs + bondLen;
                        for (i = bs; i < end; i += 2) {
                            colorBuffer[3 * i] = color.r;
                            colorBuffer[3 * i + 1] = color.g;
                            colorBuffer[3 * i + 2] = color.b;
                        }
                    });
                });
                this.ballsAndSticks.geometry.attributes['color'].needsUpdate = true;
                this.ballsAndSticks.geometry.attributes['position'].needsUpdate = true;
                this.ballsAndSticks.pickGeometry.attributes['position'].needsUpdate = true;
            };
            BallsAndSticksDrawing.create = function (molecule, tessalation, style, theme, pickFlag) {
                if (pickFlag === void 0) { pickFlag = LiteMol.PickFlagInfo.Empty; }
                var ret = new BallsAndSticksDrawing();
                ret.molecule = molecule;
                ret.ballsAndSticks = new Modes.Geometry.BallsAndSticksGeometry(molecule, tessalation, style === 1 /* VdwSpheres */, pickFlag);
                ret.applyTheme(theme);
                ret.centroid = ret.ballsAndSticks.centroid;
                ret.radius = ret.ballsAndSticks.radius;
                ret.object = new THREE.Object3D();
                var material = LiteMol.MaterialsHelper.getPhongVertexColorMaterial();
                ret.object.add(new THREE.Mesh(ret.ballsAndSticks.geometry, material));
                ret.highlightObject = ret.highlighter.object;
                var pickMaterial = LiteMol.MaterialsHelper.getPickMaterial();
                ret.pickObject = new THREE.Mesh(ret.ballsAndSticks.pickGeometry, pickMaterial);
                ret.disposeList.push(ret.highlighter, ret.ballsAndSticks, material, pickMaterial);
                return ret;
            };
            return BallsAndSticksDrawing;
        })(LiteMol.Drawing);
        Modes.BallsAndSticksDrawing = BallsAndSticksDrawing;
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        var Geometry;
        (function (Geometry) {
            var Helpers;
            (function (Helpers) {
                (function (CartoonResidueType) {
                    CartoonResidueType[CartoonResidueType["Amino"] = 0] = "Amino";
                    CartoonResidueType[CartoonResidueType["Nucleotide"] = 1] = "Nucleotide";
                    CartoonResidueType[CartoonResidueType["Het"] = 2] = "Het";
                })(Helpers.CartoonResidueType || (Helpers.CartoonResidueType = {}));
                var CartoonResidueType = Helpers.CartoonResidueType;
                ;
                (function (CartoonSecondaryType) {
                    CartoonSecondaryType[CartoonSecondaryType["Sheet"] = 0] = "Sheet";
                    CartoonSecondaryType[CartoonSecondaryType["Helix"] = 1] = "Helix";
                    CartoonSecondaryType[CartoonSecondaryType["Strand"] = 2] = "Strand";
                    CartoonSecondaryType[CartoonSecondaryType["None"] = 3] = "None";
                })(Helpers.CartoonSecondaryType || (Helpers.CartoonSecondaryType = {}));
                var CartoonSecondaryType = Helpers.CartoonSecondaryType;
                ;
                var CartoonResidue = (function () {
                    function CartoonResidue(molecule, residue, secondaryType) {
                        this.serialIdentifier = 0;
                        this.control1Position = null;
                        this.control2Position = null;
                        this.strandNitrogenPosition = null;
                        this.isStructureStart = false;
                        this.isStructureEnd = false;
                        var isHet = false, controlPoints = {}, residueName = (molecule.getters.getResidueName(residue) || "UNK").toUpperCase();
                        this.originalResidue = residue;
                        this.id = molecule.getters.getResidueId(residue);
                        switch (secondaryType) {
                            case 1 /* Helix */:
                                this.secondaryType = 1 /* Helix */;
                                break;
                            case 0 /* Sheet */:
                                this.secondaryType = 0 /* Sheet */;
                                break;
                            default:
                                this.secondaryType = 3 /* None */;
                                break;
                        }
                        var getName = molecule.getters.getAtomName, getHet = molecule.getters.isAtomHet;
                        _.forEach(molecule.getters.getResidueAtomIds(residue), function (id) {
                            var atom = molecule.getAtomById(id), name = getName(atom);
                            if (getHet(atom)) {
                                isHet = true;
                                return;
                            }
                            if (!name)
                                return;
                            name = name.toUpperCase();
                            if (CartoonResidue.controlNames[name]) {
                                controlPoints[name] = molecule.getters.getAtomPosition(atom);
                            }
                        });
                        if (!isHet) {
                            if (controlPoints["CA"] && controlPoints["O"]) {
                                this.control1Position = controlPoints["CA"];
                                this.control2Position = controlPoints["O"];
                                this.residueType = 0 /* Amino */;
                            }
                            else if (controlPoints["O5'"] && controlPoints["C3'"] && controlPoints["N3"]) {
                                this.control1Position = controlPoints["O5'"];
                                this.control2Position = controlPoints["C3'"];
                                this.strandNitrogenPosition = controlPoints["N3"];
                                this.secondaryType = 2 /* Strand */;
                                this.residueType = 1 /* Nucleotide */;
                            }
                            else {
                                this.residueType = 2 /* Het */;
                            }
                        }
                        else {
                            this.residueType = 2 /* Het */;
                        }
                        this.isWater = residueName === "HOH" || residueName === "WTR";
                    }
                    CartoonResidue.controlNames = { "CA": true, "O": true, "O5'": true, "C3'": true, "N3": true };
                    return CartoonResidue;
                })();
                Helpers.CartoonResidue = CartoonResidue;
                var CartoonElementState = (function () {
                    function CartoonElementState() {
                        this.positionCounts = 0;
                        this.isHelix = [false, false];
                        this.uPositions = [new THREE.Vector3(), new THREE.Vector3()];
                        this.vPositions = [new THREE.Vector3(), new THREE.Vector3()];
                        this.controlCounts = 0;
                        this.pPositions = [];
                        this.dPositions = [];
                    }
                    CartoonElementState.prototype.addResidue = function (r) {
                        var offset = this.positionCounts + 2;
                        this.isHelix[offset] = r.secondaryType === 1 /* Helix */;
                        this.uPositions[offset] = new THREE.Vector3().fromArray(r.control1Position);
                        this.vPositions[offset] = new THREE.Vector3().fromArray(r.control2Position);
                        this.positionCounts++;
                    };
                    CartoonElementState.prototype.addControlPoint = function (p, d) {
                        this.pPositions[this.controlCounts] = p;
                        this.dPositions[this.controlCounts] = d;
                        this.controlCounts++;
                    };
                    return CartoonElementState;
                })();
                var CartoonElement = (function () {
                    function CartoonElement(residues, linearSegmentCount) {
                        this.linearSegmentCount = linearSegmentCount;
                        this.controlPoints = [];
                        this.torsionVectors = [];
                        this.normalVectors = [];
                        this.residues = residues;
                        residues[0].isStructureStart = true;
                        var len = residues.length;
                        for (var i = 0; i < len - 1; i++) {
                            if (residues[i].secondaryType !== residues[i + 1].secondaryType) {
                                residues[i].isStructureEnd = true;
                                residues[i + 1].isStructureStart = true;
                            }
                        }
                        residues[residues.length - 1].isStructureEnd = true;
                        this.createControlPoints();
                    }
                    CartoonElement.prototype.createControlPoints = function () {
                        var state = new CartoonElementState();
                        this.initPositions(state);
                        this.initControlsPoints(state);
                        this.computeSplines(state);
                    };
                    CartoonElement.prototype.initPositions = function (state) {
                        var a, b, c, len = this.residues.length;
                        _.forEach(this.residues, function (r) { return state.addResidue(r); });
                        state.isHelix[0] = state.isHelix[2];
                        state.isHelix[1] = state.isHelix[3];
                        state.isHelix[state.isHelix.length] = state.isHelix[state.isHelix.length - 1];
                        state.isHelix[state.isHelix.length] = state.isHelix[state.isHelix.length - 2];
                        a = 2;
                        b = 3;
                        c = 4;
                        if (this.residues[0].secondaryType !== 2 /* Strand */) {
                            CartoonElement.reflectPositions(state.uPositions, 0, 1, a, b, b, c, 0.4, 0.6);
                            CartoonElement.reflectPositions(state.vPositions, 0, 1, a, b, b, c, 0.4, 0.6);
                        }
                        else {
                            CartoonElement.reflectPositions(state.uPositions, 1, 0, a, b, b, c, 0.5, 0.5);
                            CartoonElement.reflectPositions(state.vPositions, 1, 0, a, b, b, c, 0.5, 0.5);
                        }
                        a = len + 1;
                        b = len;
                        c = len - 1;
                        if (this.residues[len - 1].secondaryType !== 2 /* Strand */) {
                            CartoonElement.reflectPositions(state.uPositions, len + 2, len + 3, a, b, b, c, 0.4, 0.6);
                            CartoonElement.reflectPositions(state.vPositions, len + 2, len + 3, a, b, b, c, 0.4, 0.6);
                        }
                        else {
                            CartoonElement.reflectPositions(state.uPositions, len + 2, len + 3, a, b, b, c, 0.5, 0.5);
                            CartoonElement.reflectPositions(state.vPositions, len + 2, len + 3, a, b, b, c, 0.5, 0.5);
                        }
                    };
                    CartoonElement.prototype.initControlsPoints = function (state) {
                        var previousD = new THREE.Vector3(), len = state.uPositions.length - 1, a = new THREE.Vector3(), b = new THREE.Vector3(), c = new THREE.Vector3(), d = new THREE.Vector3();
                        for (var i = 0; i < len; i++) {
                            var ca1 = state.uPositions[i], o1 = state.vPositions[i], ca2 = state.uPositions[i + 1];
                            var p = new THREE.Vector3((ca1.x + ca2.x) / 2, (ca1.y + ca2.y) / 2, (ca1.z + ca2.z) / 2);
                            a.subVectors(ca2, ca1);
                            b.subVectors(o1, ca1);
                            c.crossVectors(a, b);
                            d.crossVectors(c, a);
                            c.normalize();
                            d.normalize();
                            if (state.isHelix[i] && state.isHelix[i + 1]) {
                                p.set(p.x + 1.5 * c.x, p.y + 1.5 * c.y, p.z + 1.5 * c.z);
                            }
                            if (i > 0 && d.angleTo(previousD) > Math.PI / 2) {
                                d.negate();
                            }
                            previousD.copy(d);
                            state.addControlPoint(p, new THREE.Vector3().addVectors(p, d));
                        }
                    };
                    CartoonElement.prototype.computeSplines = function (state) {
                        var previousControlPoint, controlPoint, torsionPoint, len = this.residues.length, pPositions = state.pPositions, dPositions = state.dPositions, p1, p2, p3, p4, d1, d2, d3, d4, previousTorsionPoint, extrapolatedControlPoint;
                        for (var i = 0; i < len; i++) {
                            p1 = pPositions[i];
                            p2 = pPositions[i + 1];
                            p3 = pPositions[i + 2];
                            p4 = pPositions[i + 3];
                            d1 = dPositions[i];
                            d2 = dPositions[i + 1];
                            d3 = dPositions[i + 2];
                            d4 = dPositions[i + 3];
                            for (var j = 1; j <= this.linearSegmentCount; j++) {
                                var t = j * 1.0 / this.linearSegmentCount;
                                if (t < 0.5) {
                                    controlPoint = CartoonElement.spline(p1, p2, p3, t + 0.5);
                                    torsionPoint = CartoonElement.spline(d1, d2, d3, t + 0.5);
                                }
                                else {
                                    controlPoint = CartoonElement.spline(p2, p3, p4, t - 0.5);
                                    torsionPoint = CartoonElement.spline(d2, d3, d4, t - 0.5);
                                }
                                if (i === 0 && j === 1) {
                                    previousControlPoint = CartoonElement.spline(p1, p2, p3, 0.5);
                                    previousTorsionPoint = CartoonElement.spline(d1, d2, d3, 0.5);
                                    extrapolatedControlPoint = CartoonElement.reflect(previousControlPoint, controlPoint, 1);
                                    this.addSplineNode(extrapolatedControlPoint, previousControlPoint, previousTorsionPoint);
                                }
                                this.addSplineNode(previousControlPoint, controlPoint, torsionPoint);
                                previousControlPoint = controlPoint;
                            }
                        }
                    };
                    CartoonElement.prototype.addSplineNode = function (previousControlPoint, controlPoint, torsionPoint) {
                        var offset = this.controlPoints.length;
                        this.controlPoints[offset] = controlPoint;
                        var torsionVector = new THREE.Vector3().subVectors(torsionPoint, controlPoint);
                        torsionVector.normalize();
                        this.torsionVectors[offset] = torsionVector;
                        var controlVector = new THREE.Vector3().subVectors(controlPoint, previousControlPoint);
                        var normalVector = new THREE.Vector3().crossVectors(torsionVector, controlVector);
                        normalVector.normalize();
                        this.normalVectors[offset] = normalVector;
                    };
                    CartoonElement.reflectPositions = function (xs, u, v, a, b, c, d, r1, r2) {
                        xs[u] = CartoonElement.reflect(xs[a], xs[b], r1);
                        xs[v] = CartoonElement.reflect(xs[c], xs[d], r2);
                    };
                    CartoonElement.reflect = function (p1, p2, amount) {
                        return new THREE.Vector3(p1.x - amount * (p2.x - p1.x), p1.y - amount * (p2.y - p1.y), p1.z - amount * (p2.z - p1.z));
                    };
                    CartoonElement.spline = function (p1, p2, p3, t) {
                        var a = Math.pow(1 - t, 2) / 2;
                        var c = Math.pow(t, 2) / 2;
                        var b = 1 - a - c;
                        var x = a * p1.x + b * p2.x + c * p3.x;
                        var y = a * p1.y + b * p2.y + c * p3.y;
                        var z = a * p1.z + b * p2.z + c * p3.z;
                        return new THREE.Vector3(x, y, z);
                    };
                    return CartoonElement;
                })();
                Helpers.CartoonElement = CartoonElement;
                var CartoonRepresentation = (function () {
                    function CartoonRepresentation(molecule, ignoreWaters, isTrace, linearSegmentCount) {
                        if (linearSegmentCount === void 0) { linearSegmentCount = 12; }
                        this.linearSegmentCount = linearSegmentCount;
                        this.molecule = molecule;
                        var getResidueId = molecule.getters.getResidueId, residues = molecule.getters.getSortedResidues(molecule.data), cartoonResidues = [];
                        if (residues.length === 0) {
                            this.hetMolecule = molecule;
                            this.residues = [];
                            this.elements = [];
                            return;
                        }
                        var secondary = molecule.getters.getSecondaryStructure(molecule.data).slice(0);
                        secondary = secondary.sort(function (a, b) {
                            return LiteMol.MoleculeHelpers.compareResidueIds(a.startResidue, b.startResidue);
                        });
                        var currentResidue = residues[0], currentResidueId = getResidueId(currentResidue), currentResidueIndex = 0;
                        _.forEach(secondary, function (structure) {
                            while (LiteMol.MoleculeHelpers.compareResidueIds(currentResidueId, structure.startResidue) < 0) {
                                cartoonResidues.push(new CartoonResidue(molecule, currentResidue, 2 /* None */));
                                currentResidue = residues[++currentResidueIndex];
                                if (!currentResidue)
                                    break;
                                currentResidueId = getResidueId(currentResidue);
                            }
                            if (!currentResidue)
                                return false;
                            while (LiteMol.MoleculeHelpers.compareResidueIds(currentResidueId, structure.endResidue) <= 0) {
                                cartoonResidues.push(new CartoonResidue(molecule, currentResidue, structure.type));
                                currentResidue = residues[++currentResidueIndex];
                                if (!currentResidue)
                                    break;
                                currentResidueId = getResidueId(currentResidue);
                            }
                        });
                        while (currentResidue) {
                            cartoonResidues.push(new CartoonResidue(molecule, currentResidue, 2 /* None */));
                            currentResidue = residues[++currentResidueIndex];
                        }
                        if (isTrace) {
                            _.forEach(cartoonResidues, function (residue) {
                                if (residue.secondaryType === 1 /* Helix */ || residue.secondaryType === 0 /* Sheet */) {
                                    residue.secondaryType = 3 /* None */;
                                }
                            });
                        }
                        else {
                            if (secondary.length === 0 && cartoonResidues.length > 0) {
                                CartoonRepresentation.assignSecondary(cartoonResidues);
                            }
                        }
                        if (ignoreWaters) {
                            cartoonResidues = _.filter(cartoonResidues, function (r) { return !r.isWater; });
                        }
                        this.elements = CartoonRepresentation.createElements(cartoonResidues, linearSegmentCount);
                        this.residues = cartoonResidues;
                        this.makeHetMolecule();
                    }
                    CartoonRepresentation.assignSecondary = function (residues) {
                        var currentTrace = [], previousResidue;
                        _.forEach(residues, function (residue, index) {
                            var isAmino = residue.residueType === 0 /* Amino */;
                            if (isAmino) {
                                if (!previousResidue || residue.id.chain === previousResidue.id.chain) {
                                    currentTrace[currentTrace.length] = residue;
                                    previousResidue = residue;
                                    return;
                                }
                            }
                            if (currentTrace.length > 3) {
                                CartoonRepresentation.assignSecondaryToAplhaTrace(currentTrace);
                            }
                            if (currentTrace.length > 0) {
                                currentTrace = isAmino ? [residue] : [];
                            }
                            previousResidue = residue;
                        });
                        if (currentTrace.length > 3) {
                            CartoonRepresentation.assignSecondaryToAplhaTrace(currentTrace);
                        }
                    };
                    CartoonRepresentation.assignSecondaryToAplhaTrace = function (trace) {
                        var len = trace.length, r, q, i, j, k;
                        var sheets = 0, helices = 0;
                        for (i = 0; i < len; i++) {
                            r = trace[i];
                            if (CartoonRepresentation.zhangSkolnickSS(trace, i, CartoonRepresentation.helixDistance, CartoonRepresentation.helixDelta)) {
                                r.secondaryType = 1 /* Helix */;
                                helices++;
                            }
                            else if (CartoonRepresentation.zhangSkolnickSS(trace, i, CartoonRepresentation.sheetDistance, CartoonRepresentation.sheetDelta)) {
                                r.secondaryType = 0 /* Sheet */;
                                sheets++;
                            }
                        }
                        for (i = 0; i < len; i++) {
                            r = trace[i];
                            if (r.secondaryType === 3 /* None */)
                                continue;
                            j = i + 1;
                            while (j < len && trace[j].secondaryType === r.secondaryType) {
                                j++;
                            }
                            if (j - i > 2) {
                                i = j;
                                continue;
                            }
                            for (k = i; k < j; k++) {
                                trace[k].secondaryType = 3 /* None */;
                            }
                        }
                    };
                    CartoonRepresentation.zhangSkolnickSS = function (trace, i, distances, delta) {
                        var j, k, len = trace.length, d;
                        for (j = Math.max(0, i - 2); j <= i; ++j) {
                            for (k = 2; k < 5; ++k) {
                                if (j + k >= len) {
                                    continue;
                                }
                                CartoonRepresentation.pos1.fromArray(trace[j].control1Position);
                                CartoonRepresentation.pos2.fromArray(trace[j + k].control1Position);
                                var d = CartoonRepresentation.pos1.distanceTo(CartoonRepresentation.pos2);
                                if (Math.abs(d - distances[k - 2]) > delta) {
                                    return false;
                                }
                            }
                        }
                        return true;
                    };
                    CartoonRepresentation.prototype.makeHetMolecule = function () {
                        var atoms = {}, bonds = [], molecule = this.molecule, hasAtoms = false;
                        _.forEach(this.residues, function (residue) {
                            if (residue.residueType !== 2 /* Het */)
                                return;
                            hasAtoms = true;
                            _.forEach(molecule.getters.getResidueAtomIds(residue.originalResidue), function (id) {
                                atoms[id] = molecule.getAtomById(id);
                            });
                        });
                        if (!hasAtoms) {
                            this.hetMolecule = undefined;
                            return;
                        }
                        _.forEach(molecule.getBonds(), function (bond) {
                            var info = molecule.getters.getBondInfo(bond);
                            if (atoms[info.aId] && atoms[info.bId])
                                bonds.push(bond);
                        });
                        this.hetMolecule = new LiteMol.Molecule({ atoms: atoms, bonds: bonds }, {
                            getAtoms: function (data) {
                                return data.atoms;
                            },
                            getAtomId: function (atom) {
                                return atom.Id;
                            },
                            getAtomById: function (data, id) {
                                return data.atoms[id];
                            },
                            getAtomPosition: function (atom) {
                                return atom.Position;
                            },
                            getAtomName: function (atom) {
                                return atom.Name ? atom.Name : atom.Symbol;
                            },
                            getAtomRadius: molecule.getters.getAtomRadius,
                            getAtomSymbol: molecule.getters.getAtomSymbol,
                            isAtomHet: function (atom) {
                                return atom.RecordType !== "ATOM";
                            },
                            getBonds: function (data) {
                                return data.bonds;
                            },
                            getBondInfo: function (bond) {
                                return { aId: bond.A, bId: bond.B, type: bond.Type };
                            }
                        });
                    };
                    CartoonRepresentation.createElements = function (residues, linearSegmentCount) {
                        var elements = [], currentElement = [], previousResidue = undefined;
                        _.forEach(residues, function (residue, index) {
                            residue.serialIdentifier = index;
                            if (residue.residueType === 2 /* Het */) {
                                if (currentElement.length > 0) {
                                    if (currentElement.length > 3) {
                                        elements.push(new CartoonElement(currentElement, linearSegmentCount));
                                    }
                                    else {
                                        _.forEach(currentElement, function (r) { return r.residueType = 2 /* Het */; });
                                    }
                                    currentElement = [];
                                }
                            }
                            else {
                                if (previousResidue && previousResidue.id.chain !== residue.id.chain && currentElement.length > 0) {
                                    if (currentElement.length > 3) {
                                        elements.push(new CartoonElement(currentElement, linearSegmentCount));
                                    }
                                    else {
                                        _.forEach(currentElement, function (r) { return r.residueType = 2 /* Het */; });
                                    }
                                    currentElement = [];
                                }
                                currentElement.push(residue);
                                previousResidue = residue;
                            }
                        });
                        if (currentElement.length > 3) {
                            elements.push(new CartoonElement(currentElement, linearSegmentCount));
                        }
                        else {
                            _.forEach(currentElement, function (r) { return r.residueType = 2 /* Het */; });
                        }
                        return elements;
                    };
                    CartoonRepresentation.helixDistance = [5.45, 5.18, 6.37];
                    CartoonRepresentation.helixDelta = 2.1;
                    CartoonRepresentation.sheetDistance = [6.1, 10.4, 13.0];
                    CartoonRepresentation.sheetDelta = 1.42;
                    CartoonRepresentation.pos1 = new THREE.Vector3();
                    CartoonRepresentation.pos2 = new THREE.Vector3();
                    return CartoonRepresentation;
                })();
                Helpers.CartoonRepresentation = CartoonRepresentation;
            })(Helpers = Geometry.Helpers || (Geometry.Helpers = {}));
        })(Geometry = Modes.Geometry || (Modes.Geometry = {}));
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        "use strict";
        (function (CartoonsDrawingType) {
            CartoonsDrawingType[CartoonsDrawingType["Default"] = 0] = "Default";
            CartoonsDrawingType[CartoonsDrawingType["AlphaTrace"] = 1] = "AlphaTrace";
        })(Modes.CartoonsDrawingType || (Modes.CartoonsDrawingType = {}));
        var CartoonsDrawingType = Modes.CartoonsDrawingType;
        ;
        var CartoonsDrawingParameters = (function () {
            function CartoonsDrawingParameters(showHet, shotWaters, drawingType) {
                if (showHet === void 0) { showHet = true; }
                if (shotWaters === void 0) { shotWaters = true; }
                if (drawingType === void 0) { drawingType = 0 /* Default */; }
                this.showHet = showHet;
                this.shotWaters = shotWaters;
                this.drawingType = drawingType;
            }
            CartoonsDrawingParameters.Default = new CartoonsDrawingParameters();
            return CartoonsDrawingParameters;
        })();
        Modes.CartoonsDrawingParameters = CartoonsDrawingParameters;
        var CartoonsDrawing = (function (_super) {
            __extends(CartoonsDrawing, _super);
            function CartoonsDrawing() {
                _super.apply(this, arguments);
                this.het = undefined;
                this.highlighter = new Modes.Geometry.CartoonsHighlighter();
            }
            CartoonsDrawing.prototype.addedToSceneInternal = function () {
                if (this.het) {
                    this.het.addedToScene(this.sceneId);
                }
                var buffer = this.cartoons.pickGeometry.attributes['pColor'].array, len = Math.floor(buffer.length / 4);
                for (var i = 0; i < len; i++) {
                    buffer[4 * i + 3] = this.sceneId / 255.0;
                }
                this.cartoons.pickGeometry.attributes['pColor'].needsUpdate = true;
            };
            CartoonsDrawing.prototype.getPickElement = function (pickId) {
                if (pickId & CartoonsDrawing.hetPickMask && this.het) {
                    return this.het.getPickElement(pickId & ~CartoonsDrawing.hetPickMask);
                }
                var pick = this.cartoons.residuesInfo[pickId];
                if (!pick)
                    return null;
                return { object: this.molecule, element: pick.residue };
            };
            CartoonsDrawing.prototype.highlightElement = function (pickId) {
                var updated;
                if (pickId & CartoonsDrawing.hetPickMask && this.het) {
                    updated = this.highlighter.clear();
                    return this.het.highlightElement(pickId & ~CartoonsDrawing.hetPickMask) || updated;
                }
                if (this.het)
                    updated = this.het.clearHighlight();
                if (this.highlighter.isElementHighlighted(pickId))
                    return updated;
                var residue = this.cartoons.residuesInfo[pickId];
                if (residue) {
                    return this.highlighter.highlight(pickId, this.theme.highlightColor, {
                        residue: residue,
                        geometry: this.cartoons.geometry
                    }) || updated;
                }
                return this.highlighter.clear() || updated;
            };
            CartoonsDrawing.prototype.clearHighlight = function () {
                var retHet = false, ret;
                if (this.het) {
                    retHet = this.het.clearHighlight();
                }
                ret = this.highlighter.clear();
                return retHet || ret;
            };
            CartoonsDrawing.prototype.applyThemeToCartoons = function () {
                var _this = this;
                var buffer = this.cartoons.geometry.attributes['color'].array, color = { r: 0.1, g: 0.1, b: 0.1 };
                _.forEach(this.cartoons.residuesInfo, function (info) {
                    if (info.vertexCount === 0)
                        return;
                    var i;
                    _this.theme.setResidueColor(_this.molecule, info.residue, color);
                    for (i = info.startVertex + info.vertexCount - 1; i >= info.startVertex; i--) {
                        buffer[3 * i] = color.r;
                        buffer[3 * i + 1] = color.g;
                        buffer[3 * i + 2] = color.b;
                    }
                });
                this.cartoons.geometry.attributes['color'].needsUpdate = true;
            };
            CartoonsDrawing.prototype.applyTheme = function (theme) {
                _super.prototype.applyTheme.call(this, theme);
                this.applyThemeToCartoons();
                if (this.het) {
                    this.het.applyTheme(theme);
                }
            };
            CartoonsDrawing.create = function (molecule, tessalation, theme, params) {
                if (params === void 0) { params = CartoonsDrawingParameters.Default; }
                var linearSegments, radialSements;
                switch (tessalation) {
                    case 0:
                        linearSegments = 6;
                        radialSements = 5;
                        break;
                    case 1:
                        linearSegments = 10;
                        radialSements = 8;
                        break;
                    case 2:
                        linearSegments = 12;
                        radialSements = 10;
                        break;
                    case 3:
                        linearSegments = 16;
                        radialSements = 14;
                        break;
                    default:
                        linearSegments = 18;
                        radialSements = 16;
                        break;
                }
                var representation = new Modes.Geometry.Helpers.CartoonRepresentation(molecule, !params.shotWaters, params.drawingType === 1 /* AlphaTrace */, linearSegments);
                var ret = new CartoonsDrawing();
                ret.molecule = molecule;
                if (representation.hetMolecule && params.showHet) {
                    ret.het = Modes.BallsAndSticksDrawing.create(representation.hetMolecule, tessalation, 0 /* BallsAndSticks */, theme, new LiteMol.PickFlagInfo(1, 1));
                }
                ret.cartoons = new Modes.Geometry.CartoonsGeometry(representation, {
                    radialSegmentCount: radialSements,
                    tessalation: tessalation
                });
                var material = LiteMol.MaterialsHelper.getPhongVertexColorMaterial(), object = new THREE.Mesh(ret.cartoons.geometry, material), pickMaterial = LiteMol.MaterialsHelper.getPickMaterial();
                if (ret.het) {
                    ret.object = new THREE.Object3D();
                    ret.object.add(object);
                    ret.object.add(ret.het.object);
                    ret.pickObject = new THREE.Object3D();
                    ret.pickObject.add(ret.het.pickObject);
                    ret.pickObject.add(new THREE.Mesh(ret.cartoons.pickGeometry, pickMaterial));
                    ret.highlightObject = new THREE.Object3D();
                    ret.highlightObject.add(ret.het.highlightObject);
                    ret.highlightObject.add(ret.highlighter.object);
                }
                else {
                    ret.object = object;
                    ret.pickObject = new THREE.Mesh(ret.cartoons.pickGeometry, pickMaterial);
                    ret.highlightObject = ret.highlighter.object;
                }
                var cx = 0.0, cy = 0.0, cz = 0.0, radiusSq = 0.0, atomCount = 0, atoms = molecule.getAtoms();
                _.forEach(atoms, function (atom) {
                    var pos = molecule.getters.getAtomPosition(atom);
                    cx += pos[0];
                    cy += pos[1];
                    cz += pos[2];
                    atomCount++;
                });
                if (atomCount > 0) {
                    cx /= atomCount;
                    cy /= atomCount;
                    cz /= atomCount;
                    _.forEach(atoms, function (atom) {
                        var pos = molecule.getters.getAtomPosition(atom);
                        var dx = cx - pos[0], dy = cy - pos[1], dz = cz - pos[2], mag = dx * dx + dy * dy + dz * dz;
                        if (mag > radiusSq)
                            radiusSq = mag;
                    });
                }
                ret.centroid = new THREE.Vector3(cx, cy, cz);
                ret.radius = Math.sqrt(radiusSq);
                ret.theme = theme;
                ret.applyThemeToCartoons();
                ret.disposeList.push(ret.cartoons, material, ret.highlighter, pickMaterial);
                if (ret.het)
                    ret.disposeList.push(ret.het);
                return ret;
            };
            CartoonsDrawing.hetPickMask = 1 << 23;
            return CartoonsDrawing;
        })(LiteMol.Drawing);
        Modes.CartoonsDrawing = CartoonsDrawing;
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        var Geometry;
        (function (Geometry) {
            var CartoonsGeometryParams = (function () {
                function CartoonsGeometryParams() {
                    this.radialSegmentCount = 10;
                    this.turnWidth = 0.2;
                    this.strandWidth = 0.33;
                    this.strandLineWidth = 0.18;
                    this.helixWidth = 1.4;
                    this.helixHeight = 0.25;
                    this.sheetWidth = 1.2;
                    this.sheetHeight = 0.25;
                    this.arrowWidth = 1.6;
                    this.tessalation = 2;
                }
                CartoonsGeometryParams.Default = new CartoonsGeometryParams();
                return CartoonsGeometryParams;
            })();
            Geometry.CartoonsGeometryParams = CartoonsGeometryParams;
            var CartoonsGeometryResidueVertexInfo = (function () {
                function CartoonsGeometryResidueVertexInfo(residue) {
                    this.residue = residue;
                    this.startVertex = 0;
                    this.vertexCount = 0;
                    this.startTriangle = 0;
                    this.triangleCount = 0;
                }
                return CartoonsGeometryResidueVertexInfo;
            })();
            Geometry.CartoonsGeometryResidueVertexInfo = CartoonsGeometryResidueVertexInfo;
            var CartoonsGeometryState = (function () {
                function CartoonsGeometryState(params) {
                    this.params = params;
                    this.residueIndex = 0;
                    this.verticesDone = 0;
                    this.trianglesDone = 0;
                    this.vertexBuffer = [];
                    this.normalBuffer = [];
                    this.indexBuffer = [];
                    this.info = [];
                    this.translationMatrix = new THREE.Matrix4();
                    this.scaleMatrix = new THREE.Matrix4();
                    this.rotationMatrix = new THREE.Matrix4();
                    this.invMatrix = new THREE.Matrix4();
                }
                CartoonsGeometryState.prototype.addVertex = function (v, n) {
                    var offset = 3 * this.verticesDone, vb = this.vertexBuffer;
                    vb[offset] = v.x;
                    vb[offset + 1] = v.y;
                    vb[offset + 2] = v.z;
                    vb = this.normalBuffer;
                    vb[offset] = n.x;
                    vb[offset + 1] = n.y;
                    vb[offset + 2] = n.z;
                    this.verticesDone++;
                };
                CartoonsGeometryState.prototype.addTriangle = function (i, j, k) {
                    var offset = 3 * this.trianglesDone, ib = this.indexBuffer;
                    ib[offset] = i;
                    ib[offset + 1] = j;
                    ib[offset + 2] = k;
                    this.trianglesDone++;
                };
                CartoonsGeometryState.prototype.addTriangles = function (i, j, k, u, v, w) {
                    var offset = 3 * this.trianglesDone, ib = this.indexBuffer;
                    ib[offset] = i;
                    ib[offset + 1] = j;
                    ib[offset + 2] = k;
                    ib[offset + 3] = u;
                    ib[offset + 4] = v;
                    ib[offset + 5] = w;
                    this.trianglesDone += 2;
                };
                return CartoonsGeometryState;
            })();
            var CartoonsGeometry = (function (_super) {
                __extends(CartoonsGeometry, _super);
                function CartoonsGeometry(representation, parameters) {
                    if (parameters === void 0) { parameters = CartoonsGeometryParams.Default; }
                    _super.call(this);
                    var params = _.extend({}, CartoonsGeometryParams.Default, params);
                    var state = new CartoonsGeometryState(params), strandLineTemplate = null;
                    _.forEach(representation.residues, function (residue) {
                        state.info[residue.serialIdentifier] = new CartoonsGeometryResidueVertexInfo(residue.originalResidue);
                    });
                    _.forEach(representation.elements, function (element) {
                        _.forEach(element.residues, function (residue, index) {
                            var info = state.info[residue.serialIdentifier];
                            state.residueIndex = index;
                            info.startVertex = state.verticesDone;
                            info.startTriangle = state.trianglesDone;
                            switch (residue.secondaryType) {
                                case 1 /* Helix */:
                                    CartoonsGeometry.addTube(element, state, params.helixWidth, params.helixHeight);
                                    if (residue.isStructureStart || residue.isStructureEnd) {
                                        CartoonsGeometry.addTubeCap(element, state, params.helixWidth, params.helixHeight, residue);
                                    }
                                    break;
                                case 0 /* Sheet */:
                                    CartoonsGeometry.addSheet(element, state, residue);
                                    if (residue.isStructureStart || residue.isStructureEnd) {
                                        CartoonsGeometry.addSheetCap(element, state, residue);
                                    }
                                    break;
                                case 2 /* Strand */:
                                    CartoonsGeometry.addTube(element, state, params.strandWidth, params.strandWidth);
                                    if (residue.isStructureStart || residue.isStructureEnd) {
                                        CartoonsGeometry.addTubeCap(element, state, params.strandWidth, params.strandWidth, residue);
                                    }
                                    if (!strandLineTemplate) {
                                        strandLineTemplate = CartoonsGeometry.getStrandLineTemplate(params.strandLineWidth, params.tessalation);
                                    }
                                    CartoonsGeometry.addStrandLine(element, state, strandLineTemplate, residue);
                                    break;
                                default:
                                    CartoonsGeometry.addTube(element, state, params.turnWidth, params.turnWidth);
                                    if (residue.isStructureStart || residue.isStructureEnd) {
                                        CartoonsGeometry.addTubeCap(element, state, params.turnWidth, params.turnWidth, residue);
                                    }
                                    break;
                            }
                            info.vertexCount = state.verticesDone - info.startVertex;
                            info.triangleCount = state.trianglesDone - info.startTriangle;
                        });
                    });
                    if (strandLineTemplate)
                        strandLineTemplate.dispose();
                    this.createGeometry(state);
                    this.residuesInfo = state.info;
                }
                CartoonsGeometry.prototype.dispose = function () {
                    this.geometry.dispose();
                    this.pickGeometry.dispose();
                };
                CartoonsGeometry.prototype.createGeometry = function (state) {
                    var vertexBuffer = new Float32Array(state.vertexBuffer), normalBuffer = new Float32Array(state.normalBuffer), colorBuffer = new Float32Array(state.verticesDone * 3), pickColorBuffer = new Float32Array(state.verticesDone * 4), indexBuffer = new Uint32Array(state.indexBuffer);
                    var geometry = new THREE.BufferGeometry();
                    geometry.addAttribute('position', new THREE.BufferAttribute(vertexBuffer, 3));
                    geometry.addAttribute('normal', new THREE.BufferAttribute(normalBuffer, 3));
                    geometry.addAttribute('index', new THREE.BufferAttribute(indexBuffer, 1));
                    geometry.addAttribute('color', new THREE.BufferAttribute(colorBuffer, 3));
                    this.geometry = geometry;
                    _.forEach(state.info, function (info, index) {
                        if (info.vertexCount === 0)
                            return;
                        LiteMol.GeometryHelper.setPickColor(0, 0, index, pickColorBuffer, info.startVertex * 4);
                        var r = pickColorBuffer[info.startVertex * 4], g = pickColorBuffer[info.startVertex * 4 + 1], b = pickColorBuffer[info.startVertex * 4 + 2];
                        for (var i = info.startVertex; i < info.startVertex + info.vertexCount; i++) {
                            pickColorBuffer[i * 4] = r, pickColorBuffer[i * 4 + 1] = g, pickColorBuffer[i * 4 + 2] = b;
                        }
                    });
                    var pickGeometry = new THREE.BufferGeometry();
                    pickGeometry.addAttribute('position', new THREE.BufferAttribute(vertexBuffer, 3));
                    pickGeometry.addAttribute('index', new THREE.BufferAttribute(indexBuffer, 1));
                    pickGeometry.addAttribute('pColor', new THREE.BufferAttribute(pickColorBuffer, 4));
                    this.pickGeometry = pickGeometry;
                };
                CartoonsGeometry.addTube = function (element, state, width, height) {
                    var verticesDone = state.verticesDone, i, j, t, radialVector = new THREE.Vector3(), normalVector = new THREE.Vector3(), tempPos = new THREE.Vector3(), a = new THREE.Vector3(), b = new THREE.Vector3(), u, v, addedTriangleCount = 0, elementOffsetStart = state.residueIndex * element.linearSegmentCount, elementOffsetEnd = elementOffsetStart + element.linearSegmentCount, elementPoints = element.controlPoints, elementPointsCount = element.linearSegmentCount + 1, torsionVectors = element.torsionVectors, normalVectors = element.normalVectors, radialSegmentCount = state.params.radialSegmentCount;
                    for (i = elementOffsetStart; i <= elementOffsetEnd; i++) {
                        u = torsionVectors[i];
                        v = normalVectors[i];
                        for (j = 0; j < radialSegmentCount; j++) {
                            var t = 2 * Math.PI * j / radialSegmentCount;
                            a.copy(u);
                            b.copy(v);
                            radialVector.addVectors(a.multiplyScalar(width * Math.cos(t)), b.multiplyScalar(height * Math.sin(t)));
                            a.copy(u);
                            b.copy(v);
                            normalVector.addVectors(b.multiplyScalar(height * Math.cos(t)), a.multiplyScalar(width * Math.sin(t)));
                            normalVector.normalize();
                            tempPos.addVectors(elementPoints[i], radialVector);
                            state.addVertex(tempPos, normalVector);
                        }
                    }
                    for (i = 0; i < elementPointsCount - 1; i++) {
                        for (j = 0; j < radialSegmentCount; j++) {
                            state.addTriangles((verticesDone + i * radialSegmentCount + j), (verticesDone + (i + 1) * radialSegmentCount + (j + 1) % radialSegmentCount), (verticesDone + i * radialSegmentCount + (j + 1) % radialSegmentCount), (verticesDone + i * radialSegmentCount + j), (verticesDone + (i + 1) * radialSegmentCount + j), (verticesDone + (i + 1) * radialSegmentCount + (j + 1) % radialSegmentCount));
                        }
                    }
                };
                CartoonsGeometry.addTubeCap = function (element, state, width, height, residue) {
                    var verticesDone = state.verticesDone, t, radialVector = new THREE.Vector3(), normalVector = new THREE.Vector3(), a = new THREE.Vector3(), b = new THREE.Vector3(), u, v, addedTriangleCount = 0, elementOffsetStart = state.residueIndex * element.linearSegmentCount, elementPoints = element.controlPoints, elementPointsCount = element.linearSegmentCount + 1, torsionVectors = element.torsionVectors, normalVectors = element.normalVectors, radialSegmentCount = state.params.radialSegmentCount;
                    var normalVector = new THREE.Vector3().crossVectors(torsionVectors[elementOffsetStart], normalVectors[elementOffsetStart]);
                    if (residue.isStructureEnd)
                        normalVector.negate();
                    var offset = elementOffsetStart + (residue.isStructureStart ? 0 : (elementPointsCount - 1));
                    radialVector.copy(elementPoints[offset]);
                    state.addVertex(radialVector, normalVector);
                    u = torsionVectors[offset];
                    v = normalVectors[offset];
                    for (var i = 0; i < radialSegmentCount; i++) {
                        t = 2 * Math.PI * i / radialSegmentCount;
                        a.copy(u);
                        b.copy(v);
                        radialVector.addVectors(a.multiplyScalar(Math.cos(t) * width), b.multiplyScalar(Math.sin(t) * height));
                        radialVector.add(elementPoints[offset]);
                        state.addVertex(radialVector, normalVector);
                        if (residue.isStructureStart) {
                            state.addTriangle(verticesDone, (verticesDone + i + 1), (verticesDone + (i + 1) % radialSegmentCount + 1));
                        }
                        else {
                            state.addTriangle((verticesDone), (verticesDone + (i + 1) % radialSegmentCount + 1), (verticesDone + i + 1));
                        }
                    }
                };
                CartoonsGeometry.addSheet = function (element, state, residue) {
                    var verticesDone = state.verticesDone, params = state.params, i, j, horizontalVector = new THREE.Vector3(), verticalVector = new THREE.Vector3(), positionVector = new THREE.Vector3(), normalOffset = new THREE.Vector3(), normalVector = new THREE.Vector3(), temp, tempNormal, addedTriangleCount = 0, elementOffsetStart = state.residueIndex * element.linearSegmentCount, elementOffsetEnd = elementOffsetStart + element.linearSegmentCount, elementPoints = element.controlPoints, elementPointsCount = element.linearSegmentCount + 1, torsionVectors = element.torsionVectors, normalVectors = element.normalVectors, radialSegmentCount = state.params.radialSegmentCount, offsetLength = 0, actualWidth;
                    var offsetLength = 0;
                    if (residue.isStructureEnd) {
                        offsetLength = params.arrowWidth / new THREE.Vector3().subVectors(elementPoints[elementOffsetEnd], elementPoints[elementOffsetStart]).length();
                    }
                    for (i = elementOffsetStart; i <= elementOffsetEnd; i++) {
                        actualWidth = !residue.isStructureEnd ? params.sheetWidth : params.arrowWidth * (1 - (i - elementOffsetStart) / element.linearSegmentCount);
                        horizontalVector.copy(torsionVectors[i]).multiplyScalar(actualWidth);
                        verticalVector.copy(normalVectors[i]).multiplyScalar(params.sheetHeight);
                        if (residue.isStructureEnd) {
                            normalOffset.crossVectors(normalVectors[i], torsionVectors[i]).multiplyScalar(offsetLength);
                        }
                        temp = elementPoints[i];
                        positionVector.addVectors(temp, horizontalVector).add(verticalVector);
                        state.addVertex(positionVector, normalVectors[i]);
                        positionVector.subVectors(temp, horizontalVector).add(verticalVector);
                        state.addVertex(positionVector, normalVectors[i]);
                        normalVector.subVectors(normalOffset, torsionVectors[i]);
                        state.addVertex(positionVector, normalVector);
                        positionVector.subVectors(temp, horizontalVector).sub(verticalVector);
                        state.addVertex(positionVector, normalVector);
                        normalVector.copy(normalVectors[i]).negate();
                        state.addVertex(positionVector, normalVector);
                        positionVector.addVectors(temp, horizontalVector).sub(verticalVector);
                        state.addVertex(positionVector, normalVector);
                        normalVector.addVectors(normalOffset, torsionVectors[i]);
                        state.addVertex(positionVector, normalVector);
                        positionVector.addVectors(temp, horizontalVector).add(verticalVector);
                        state.addVertex(positionVector, normalVector);
                    }
                    for (i = 0; i < element.linearSegmentCount; i++) {
                        for (j = 0; j < 4; j++) {
                            state.addTriangles(verticesDone + i * 8 + 2 * j, verticesDone + (i + 1) * 8 + 2 * j + 1, verticesDone + i * 8 + 2 * j + 1, verticesDone + i * 8 + 2 * j, verticesDone + (i + 1) * 8 + 2 * j, verticesDone + (i + 1) * 8 + 2 * j + 1);
                        }
                    }
                };
                CartoonsGeometry.addSheetCap = function (element, state, residue) {
                    var params = state.params, horizontalVector, verticalVector, arrowHorizontalVector, temp, addedTriangleCount = 0, elementOffsetStart = state.residueIndex * element.linearSegmentCount, elementPoint = element.controlPoints[elementOffsetStart];
                    horizontalVector = new THREE.Vector3().copy(element.torsionVectors[elementOffsetStart]).multiplyScalar(params.sheetWidth);
                    verticalVector = new THREE.Vector3().copy(element.normalVectors[elementOffsetStart]).multiplyScalar(params.sheetHeight);
                    var p1 = new THREE.Vector3().addVectors(elementPoint, horizontalVector).add(verticalVector), p2 = new THREE.Vector3().subVectors(elementPoint, horizontalVector).add(verticalVector), p3 = new THREE.Vector3().subVectors(elementPoint, horizontalVector).sub(verticalVector), p4 = new THREE.Vector3().addVectors(elementPoint, horizontalVector).sub(verticalVector);
                    if (residue.isStructureStart) {
                        CartoonsGeometry.addSheepCapSection(state, p1, p2, p3, p4);
                    }
                    else {
                        arrowHorizontalVector = new THREE.Vector3().copy(element.torsionVectors[elementOffsetStart]).multiplyScalar(params.arrowWidth);
                        var p5 = new THREE.Vector3().addVectors(elementPoint, arrowHorizontalVector).add(verticalVector), p6 = new THREE.Vector3().subVectors(elementPoint, arrowHorizontalVector).add(verticalVector), p7 = new THREE.Vector3().subVectors(elementPoint, arrowHorizontalVector).sub(verticalVector), p8 = new THREE.Vector3().addVectors(elementPoint, arrowHorizontalVector).sub(verticalVector);
                        CartoonsGeometry.addSheepCapSection(state, p5, p1, p4, p8);
                        CartoonsGeometry.addSheepCapSection(state, p2, p6, p7, p3);
                    }
                };
                CartoonsGeometry.addSheepCapSection = function (state, p1, p2, p3, p4) {
                    var addedVerticesCount = state.verticesDone, normal = new THREE.Vector3().crossVectors(new THREE.Vector3().subVectors(p2, p1), new THREE.Vector3().subVectors(p4, p1)).normalize();
                    state.addVertex(p1, normal);
                    state.addVertex(p2, normal);
                    state.addVertex(p3, normal);
                    state.addVertex(p4, normal);
                    state.addTriangles(addedVerticesCount, addedVerticesCount + 1, addedVerticesCount + 2, addedVerticesCount + 2, addedVerticesCount + 3, addedVerticesCount);
                };
                CartoonsGeometry.addStrandLine = function (element, state, strandLineTemplate, residue) {
                    var p = new THREE.Vector3(), n = new THREE.Vector3(), i, vb = strandLineTemplate.attributes['position'].array, nb = strandLineTemplate.attributes['normal'].array, ib = strandLineTemplate.attributes['index'].array, vertexStart = state.verticesDone, vertexCount = vb.length, triangleCount = ib.length, elementOffset = state.residueIndex * element.linearSegmentCount + ((0.5 * element.linearSegmentCount + 1) | 0), elementPoint = element.controlPoints[elementOffset], nDir = new THREE.Vector3().fromArray(residue.strandNitrogenPosition).sub(elementPoint), length = nDir.length();
                    nDir.normalize();
                    state.translationMatrix.makeTranslation(elementPoint.x, elementPoint.y, elementPoint.z);
                    state.scaleMatrix.makeScale(1, 1, length);
                    state.rotationMatrix.makeRotationAxis(new THREE.Vector3(-nDir.y, nDir.x, 0), Math.acos(nDir.z));
                    state.translationMatrix.multiply(state.rotationMatrix).multiply(state.scaleMatrix);
                    strandLineTemplate.applyMatrix(state.translationMatrix);
                    for (i = 0; i < vertexCount; i += 3) {
                        p.set(vb[i], vb[i + 1], vb[i + 2]);
                        n.set(nb[i], nb[i + 1], nb[i + 2]);
                        state.addVertex(p, n);
                    }
                    for (i = 0; i < triangleCount; i += 3) {
                        state.addTriangle(vertexStart + ib[i], vertexStart + ib[i + 1], vertexStart + ib[i + 2]);
                    }
                    state.invMatrix.getInverse(state.translationMatrix);
                    strandLineTemplate.applyMatrix(state.invMatrix);
                };
                CartoonsGeometry.getStrandLineTemplate = function (radius, tessalation) {
                    var capPoints, radiusPoints, geom;
                    switch (tessalation) {
                        case 0:
                            radiusPoints = 4;
                            capPoints = 1;
                            break;
                        case 1:
                            radiusPoints = 6;
                            capPoints = 2;
                            break;
                        case 2:
                            radiusPoints = 8;
                            capPoints = 4;
                            break;
                        case 3:
                            radiusPoints = 10;
                            capPoints = 6;
                            break;
                        default:
                            radiusPoints = 12;
                            capPoints = 8;
                            break;
                    }
                    var arc = [], delta = (Math.PI / 2) / capPoints;
                    for (var i = 0; i <= capPoints; i++) {
                        arc[i] = new THREE.Vector3(0, radius * Math.cos(i * delta), 0.1 * Math.sin(i * delta));
                        arc[i].z += 0.9;
                    }
                    geom = new THREE.LatheGeometry([new THREE.Vector3(0, radius, 0)].concat(arc), radiusPoints, Math.PI);
                    return LiteMol.GeometryHelper.getIndexedBufferGeometry(geom);
                };
                return CartoonsGeometry;
            })(LiteMol.GeometryBase);
            Geometry.CartoonsGeometry = CartoonsGeometry;
            var CartoonsHighlighter = (function (_super) {
                __extends(CartoonsHighlighter, _super);
                function CartoonsHighlighter() {
                    _super.apply(this, arguments);
                }
                CartoonsHighlighter.prototype.highlightInternal = function (params) {
                    var residue = params.residue, geometry = params.geometry, buffers = this.buffers, count = residue.triangleCount * 3;
                    this.indexBufferSize = count;
                    this.ensureBuffersSize();
                    var ib = this.arrays.index, ibSrc = geometry.attributes['index'].array;
                    for (var i = 0; i < count; i++) {
                        ib[i] = ibSrc[3 * residue.startTriangle + i] - residue.startVertex;
                    }
                    buffers.vertex.array = new Float32Array(geometry.attributes['position'].array.buffer, 4 * 3 * residue.startVertex, 3 * residue.vertexCount);
                    buffers.normal.array = new Float32Array(geometry.attributes['normal'].array.buffer, 4 * 3 * residue.startVertex, 3 * residue.vertexCount);
                    buffers.index.array = new Uint32Array(ib.buffer, 0, count);
                };
                return CartoonsHighlighter;
            })(LiteMol.GeometryHighlighterBase);
            Geometry.CartoonsHighlighter = CartoonsHighlighter;
        })(Geometry = Modes.Geometry || (Modes.Geometry = {}));
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Modes;
    (function (Modes) {
        var MolecularSurfaceDrawing = (function (_super) {
            __extends(MolecularSurfaceDrawing, _super);
            function MolecularSurfaceDrawing() {
                _super.apply(this, arguments);
                this.highlighter = new Modes.Geometry.MolecularSurfaceHighlighter();
            }
            MolecularSurfaceDrawing.prototype.addedToSceneInternal = function () {
                var buffer = this.surface.pickGeometry.attributes['pColor'].array, len = Math.floor(buffer.length / 4);
                for (var i = 0; i < len; i++) {
                    buffer[4 * i + 3] = this.sceneId / 255.0;
                }
                this.surface.pickGeometry.attributes['pColor'].needsUpdate = true;
            };
            MolecularSurfaceDrawing.prototype.getPickElement = function (pickId) {
                var el = this.molecule.getAtomById(pickId);
                if (el) {
                    return { object: this.molecule, element: el };
                }
                return null;
            };
            MolecularSurfaceDrawing.prototype.highlightElement = function (pickId) {
                if (this.highlighter.isElementHighlighted(pickId))
                    return false;
                var atom = this.surface.atomsInfo[pickId];
                if (atom) {
                    return this.highlighter.highlight(pickId, this.theme.highlightColor, {
                        atom: atom,
                        geometry: this.surface.geometry
                    });
                }
                return this.highlighter.clear();
            };
            MolecularSurfaceDrawing.prototype.clearHighlight = function () {
                return this.highlighter.clear();
            };
            MolecularSurfaceDrawing.prototype.applyTheme = function (theme) {
                var _this = this;
                _super.prototype.applyTheme.call(this, theme);
                var buffer = this.surface.geometry.attributes['color'].array, map = this.surface.atomsInfo, color = { r: 0.1, g: 0.1, b: 0.1 }, getAtomId = this.molecule.getters.getAtomId;
                _.forEach(this.molecule.getAtoms(), function (a) {
                    theme.setAtomColor(_this.molecule, a, color);
                    _.forEach(map[getAtomId(a)].vertices, function (offset) {
                        buffer[3 * offset] = color.r;
                        buffer[3 * offset + 1] = color.g;
                        buffer[3 * offset + 2] = color.b;
                    });
                });
                this.surface.geometry.attributes['color'].needsUpdate = true;
            };
            MolecularSurfaceDrawing.create = function (molecule, probeRadius, density, theme) {
                var ret = new MolecularSurfaceDrawing();
                ret.molecule = molecule;
                ret.surface = new Modes.Geometry.MolecularSurfaceGeometry(molecule, probeRadius, density);
                ret.applyTheme(theme);
                ret.centroid = ret.surface.centroid;
                ret.radius = ret.surface.radius;
                var material = LiteMol.MaterialsHelper.getPhongVertexColorMaterial();
                ret.object = new THREE.Mesh(ret.surface.geometry, material);
                ret.highlightObject = ret.highlighter.object;
                var pickObject = new THREE.Object3D(), pickMaterial = LiteMol.MaterialsHelper.getPickMaterial(), pickExMaterial = LiteMol.MaterialsHelper.getPickExcludeMaterial();
                pickObject.add(new THREE.Mesh(ret.surface.pickGeometry, pickMaterial));
                pickObject.add(new THREE.Mesh(ret.surface.pickExcludeGeometry, pickExMaterial));
                ret.pickObject = pickObject;
                ret.disposeList.push(ret.surface, material, ret.highlighter, pickExMaterial, pickMaterial);
                return ret;
            };
            return MolecularSurfaceDrawing;
        })(LiteMol.Drawing);
        Modes.MolecularSurfaceDrawing = MolecularSurfaceDrawing;
    })(Modes = LiteMol.Modes || (LiteMol.Modes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Themes;
    (function (Themes) {
        "use strict";
        var ChargeColoringTheme = (function () {
            function ChargeColoringTheme(moleculeGetters, values, diffValues, minValue, maxValue, ballScaling, valueGrouping, valueGroupindRadius, colors) {
                if (colors === void 0) { colors = {}; }
                this.moleculeGetters = moleculeGetters;
                this.values = values;
                this.diffValues = diffValues;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.ballScaling = ballScaling;
                this.valueGrouping = valueGrouping;
                this.valueGroupindRadius = valueGroupindRadius;
                this.highlightColor = { r: 0.9, g: 0.9, b: 0.0 };
                _.extend(this, ChargeColoringTheme.defaultColors, colors);
            }
            ChargeColoringTheme.sign = function (x) {
                return (x === 0) ? x : (x > 0) ? 1 : -1;
            };
            ChargeColoringTheme.prototype.snapValue = function (value) {
                if (this.valueGrouping) {
                    var sign = ChargeColoringTheme.sign(value);
                    value = value + sign * this.valueGroupindRadius / 2;
                    value = (Math.floor(value / this.valueGroupindRadius) + Math.ceil(value / this.valueGroupindRadius) - sign) * this.valueGroupindRadius * 0.5;
                }
                return value;
            };
            ChargeColoringTheme.prototype.interpolate = function (value, color) {
                if (isNaN(value)) {
                    color.r = this.missingColor.r;
                    color.g = this.missingColor.g;
                    color.b = this.missingColor.b;
                    return;
                }
                value = this.snapValue(value);
                if (value <= this.minValue) {
                    color.r = this.minColor.r;
                    color.g = this.minColor.g;
                    color.b = this.minColor.b;
                    return;
                }
                if (value >= this.maxValue) {
                    color.r = this.maxColor.r;
                    color.g = this.maxColor.g;
                    color.b = this.maxColor.b;
                    return;
                }
                var t, target, mid = this.midColor;
                if (value <= 0) {
                    t = value / this.minValue;
                    target = this.minColor;
                }
                else {
                    t = value / this.maxValue;
                    target = this.maxColor;
                }
                ;
                color.r = mid.r + (target.r - mid.r) * t;
                color.g = mid.g + (target.g - mid.g) * t;
                color.b = mid.b + (target.b - mid.b) * t;
            };
            ChargeColoringTheme.prototype.setAtomColor = function (moleculeData, atom, color) {
                if (this.diffValues) {
                    return this.interpolate(this.values[atom.Id] - this.diffValues[atom.Id], color);
                }
                return this.interpolate(this.values[atom.Id], color);
            };
            ChargeColoringTheme.prototype.getAtomScale = function (moleculeData, atom) {
                if (this.ballScaling) {
                    var value, t, scale;
                    if (this.diffValues) {
                        value = this.values[atom.Id] - this.diffValues[atom.Id];
                    }
                    else {
                        value = this.values[atom.Id];
                    }
                    if (isNaN(value))
                        return 0.5;
                    value = this.snapValue(value);
                    scale = Math.max(Math.abs(this.minValue), Math.abs(this.maxValue));
                    if (value <= -scale || value >= scale)
                        return 2.0;
                    if (value <= 0) {
                        t = -value / scale;
                    }
                    else {
                        t = value / scale;
                    }
                    ;
                    return 0.5 + 1.5 * t;
                }
                return this.moleculeGetters.getAtomRadius(atom);
            };
            ChargeColoringTheme.prototype.setResidueColor = function (moleculeData, residue, color) {
                var _this = this;
                var sum = 0.0;
                _.forEach(this.moleculeGetters.getResidueAtomIds(residue), function (id) {
                    var value;
                    if (_this.diffValues) {
                        value = _this.values[id] - _this.diffValues[id];
                    }
                    value = _this.values[id];
                    sum += value;
                    if (isNaN(sum))
                        return false;
                });
                this.interpolate(sum, color);
            };
            ChargeColoringTheme.defaultColors = {
                minColor: { r: 0.0, g: 0.0, b: 1.0 },
                midColor: { r: 1.0, g: 1.0, b: 1.0 },
                maxColor: { r: 1.0, g: 0.0, b: 0.0 },
                missingColor: { r: 0.0, g: 0.0, b: 0.0 },
            };
            return ChargeColoringTheme;
        })();
        Themes.ChargeColoringTheme = ChargeColoringTheme;
    })(Themes = LiteMol.Themes || (LiteMol.Themes = {}));
})(LiteMol || (LiteMol = {}));
var LiteMol;
(function (LiteMol) {
    var Themes;
    (function (Themes) {
        var DefaultTheme = (function () {
            function DefaultTheme(moleculeGetters) {
                this.moleculeGetters = moleculeGetters;
                this.highlightColor = { r: 0.9, g: 0.9, b: 0.0 };
            }
            DefaultTheme.prototype.setAtomColor = function (moleculeData, atom, color) {
                var symbol = this.moleculeGetters.getAtomSymbol(atom), newColor;
                if (!symbol || !(newColor = DefaultTheme.elementColors[symbol])) {
                    color.r = 1.0;
                    color.g = 1.0;
                    color.b = 1.0;
                    return;
                }
                color.r = newColor.r;
                color.g = newColor.g;
                color.b = newColor.b;
            };
            DefaultTheme.prototype.getAtomScale = function (moleculeData, atom) {
                return 1.0;
            };
            DefaultTheme.prototype.setResidueColor = function (moleculeData, residue, color) {
                color.r = 0.3;
                color.g = 0.5;
                color.b = 0.9;
            };
            DefaultTheme.elementColors = { "H": { "r": 1.0, "g": 1.0, "b": 1.0 }, "D": { "r": 0.50196, "g": 0.0, "b": 0.50196 }, "He": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Li": { "r": 0.8, "g": 0.50196, "b": 1.0 }, "Be": { "r": 0.76078, "g": 1.0, "b": 0.0 }, "B": { "r": 1.0, "g": 0.7098, "b": 0.7098 }, "C": { "r": 0.56471, "g": 0.56471, "b": 0.56471 }, "N": { "r": 0.18824, "g": 0.31373, "b": 0.97255 }, "O": { "r": 1.0, "g": 0.05098, "b": 0.05098 }, "F": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Ne": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Na": { "r": 0.67059, "g": 0.36078, "b": 0.94902 }, "Mg": { "r": 0.54118, "g": 1.0, "b": 0.0 }, "Al": { "r": 0.74902, "g": 0.65098, "b": 0.65098 }, "Si": { "r": 0.94118, "g": 0.78431, "b": 0.62745 }, "P": { "r": 1.0, "g": 0.50196, "b": 0.0 }, "S": { "r": 1.0, "g": 1.0, "b": 0.18824 }, "Cl": { "r": 0.12157, "g": 0.94118, "b": 0.12157 }, "Ar": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "K": { "r": 0.56078, "g": 0.25098, "b": 0.83137 }, "Ca": { "r": 0.23922, "g": 1.0, "b": 0.0 }, "Sc": { "r": 0.90196, "g": 0.90196, "b": 0.90196 }, "Ti": { "r": 0.74902, "g": 0.76078, "b": 0.78039 }, "V": { "r": 0.65098, "g": 0.65098, "b": 0.67059 }, "Cr": { "r": 0.54118, "g": 0.6, "b": 0.78039 }, "Mn": { "r": 0.61176, "g": 0.47843, "b": 0.78039 }, "Fe": { "r": 0.87843, "g": 0.4, "b": 0.2 }, "Co": { "r": 0.94118, "g": 0.56471, "b": 0.62745 }, "Ni": { "r": 0.31373, "g": 0.81569, "b": 0.31373 }, "Cu": { "r": 0.78431, "g": 0.50196, "b": 0.2 }, "Zn": { "r": 0.4902, "g": 0.50196, "b": 0.6902 }, "Ga": { "r": 0.76078, "g": 0.56078, "b": 0.56078 }, "Ge": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "As": { "r": 0.74118, "g": 0.50196, "b": 0.8902 }, "Se": { "r": 1.0, "g": 0.63137, "b": 0.0 }, "Br": { "r": 0.65098, "g": 0.16078, "b": 0.16078 }, "Kr": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Rb": { "r": 0.43922, "g": 0.18039, "b": 0.6902 }, "Sr": { "r": 0.0, "g": 1.0, "b": 0.0 }, "Y": { "r": 0.58039, "g": 1.0, "b": 1.0 }, "Zr": { "r": 0.58039, "g": 0.87843, "b": 0.87843 }, "Nb": { "r": 0.45098, "g": 0.76078, "b": 0.78824 }, "Mo": { "r": 0.32941, "g": 0.7098, "b": 0.7098 }, "Tc": { "r": 0.23137, "g": 0.61961, "b": 0.61961 }, "Ru": { "r": 0.14118, "g": 0.56078, "b": 0.56078 }, "Rh": { "r": 0.03922, "g": 0.4902, "b": 0.54902 }, "Pd": { "r": 0.0, "g": 0.41176, "b": 0.52157 }, "Ag": { "r": 0.75294, "g": 0.75294, "b": 0.75294 }, "Cd": { "r": 1.0, "g": 0.85098, "b": 0.56078 }, "In": { "r": 0.65098, "g": 0.45882, "b": 0.45098 }, "Sn": { "r": 0.4, "g": 0.50196, "b": 0.50196 }, "Sb": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Te": { "r": 0.83137, "g": 0.47843, "b": 0.0 }, "I": { "r": 0.58039, "g": 0.0, "b": 0.58039 }, "Xe": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Cs": { "r": 0.34118, "g": 0.0902, "b": 0.56078 }, "Ba": { "r": 0.0, "g": 0.78824, "b": 0.0 }, "La": { "r": 0.43922, "g": 0.83137, "b": 1.0 }, "Ce": { "r": 1.0, "g": 1.0, "b": 0.78039 }, "Pr": { "r": 0.85098, "g": 1.0, "b": 0.78039 }, "Nd": { "r": 0.78039, "g": 1.0, "b": 0.78039 }, "Pm": { "r": 0.63922, "g": 1.0, "b": 0.78039 }, "Sm": { "r": 0.56078, "g": 1.0, "b": 0.78039 }, "Eu": { "r": 0.38039, "g": 1.0, "b": 0.78039 }, "Gd": { "r": 0.27059, "g": 1.0, "b": 0.78039 }, "Tb": { "r": 0.18824, "g": 1.0, "b": 0.78039 }, "Dy": { "r": 0.12157, "g": 1.0, "b": 0.78039 }, "Ho": { "r": 0.0, "g": 1.0, "b": 0.61176 }, "Er": { "r": 0.0, "g": 0.90196, "b": 0.45882 }, "Tm": { "r": 0.0, "g": 0.83137, "b": 0.32157 }, "Yb": { "r": 0.0, "g": 0.74902, "b": 0.21961 }, "Lu": { "r": 0.0, "g": 0.67059, "b": 0.14118 }, "Hf": { "r": 0.30196, "g": 0.76078, "b": 1.0 }, "Ta": { "r": 0.30196, "g": 0.65098, "b": 1.0 }, "W": { "r": 0.12941, "g": 0.58039, "b": 0.83922 }, "Re": { "r": 0.14902, "g": 0.4902, "b": 0.67059 }, "Os": { "r": 0.14902, "g": 0.4, "b": 0.58824 }, "Ir": { "r": 0.0902, "g": 0.32941, "b": 0.52941 }, "Pt": { "r": 0.81569, "g": 0.81569, "b": 0.87843 }, "Au": { "r": 1.0, "g": 0.81961, "b": 0.13725 }, "Hg": { "r": 0.72157, "g": 0.72157, "b": 0.81569 }, "Tl": { "r": 0.65098, "g": 0.32941, "b": 0.30196 }, "Pb": { "r": 0.34118, "g": 0.34902, "b": 0.38039 }, "Bi": { "r": 0.61961, "g": 0.3098, "b": 0.7098 }, "Po": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "At": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Rn": { "r": 0.25882, "g": 0.5098, "b": 0.58824 }, "Fr": { "r": 0.25882, "g": 0.0, "b": 0.4 }, "Ra": { "r": 0.0, "g": 0.4902, "b": 0.0 }, "Ac": { "r": 0.43922, "g": 0.67059, "b": 0.98039 }, "Th": { "r": 0.0, "g": 0.72941, "b": 1.0 }, "Pa": { "r": 0.0, "g": 0.63137, "b": 1.0 }, "U": { "r": 0.0, "g": 0.56078, "b": 1.0 }, "Np": { "r": 0.0, "g": 0.50196, "b": 1.0 }, "Pu": { "r": 0.0, "g": 0.41961, "b": 1.0 }, "Am": { "r": 0.32941, "g": 0.36078, "b": 0.94902 }, "Cm": { "r": 0.47059, "g": 0.36078, "b": 0.8902 }, "Bk": { "r": 0.54118, "g": 0.3098, "b": 0.8902 }, "Cf": { "r": 0.63137, "g": 0.21176, "b": 0.83137 }, "Es": { "r": 0.70196, "g": 0.12157, "b": 0.83137 }, "Fm": { "r": 0.70196, "g": 0.12157, "b": 0.72941 }, "Md": { "r": 0.70196, "g": 0.05098, "b": 0.65098 }, "No": { "r": 0.74118, "g": 0.05098, "b": 0.52941 }, "Lr": { "r": 0.78039, "g": 0.0, "b": 0.4 }, "Rf": { "r": 0.8, "g": 0.0, "b": 0.34902 }, "Db": { "r": 0.81961, "g": 0.0, "b": 0.3098 }, "Sg": { "r": 0.85098, "g": 0.0, "b": 0.27059 }, "Bh": { "r": 0.87843, "g": 0.0, "b": 0.21961 }, "Hs": { "r": 0.90196, "g": 0.0, "b": 0.18039 }, "Mt": { "r": 0.92157, "g": 0.0, "b": 0.14902 } };
            return DefaultTheme;
        })();
        Themes.DefaultTheme = DefaultTheme;
    })(Themes = LiteMol.Themes || (LiteMol.Themes = {}));
})(LiteMol || (LiteMol = {}));
