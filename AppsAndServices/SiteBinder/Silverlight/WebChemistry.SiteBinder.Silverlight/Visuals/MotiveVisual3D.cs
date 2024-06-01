using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using WebChemistry.SiteBinder.Silverlight.DataModel;

namespace WebChemistry.SiteBinder.Silverlight.Visuals
{
    public class MotiveVisual3D : Visual3D
    {
        AtomVisual[] _atomVisuals;
        BondVisual[] _bondVisuals;
        StructureWrap _motive;
        StructureMaterial _material;
        StructureModel3D _model;
        
        public MultiMotiveVisual3D MultiMotiveVisual { get; private set; }

        public IStructure Structure { get; private set; }

        public StructureWrap Motive { get { return _motive; } }

        public Color Color { get { return _material.Color; } set { _material.Color = value; UpdateColorMode(MultiMotiveVisual.AtomColorMode); } }

        AtomDisplayMode atomDisplayMode;
        public AtomDisplayMode AtomDisplayMode
        {
            set { atomDisplayMode = value; _atomVisuals.ForEach(v => v.AtomDisplayMode = value); _bondVisuals.ForEach(v => v.AtomDisplayMode = value); }
        }
        
        public override void Register(Viewport3DBase vp)
        {
            base.Register(vp);

            _atomVisuals.ForEach(a => a.Register(vp));
            _bondVisuals.ForEach(a => a.Register(vp));
        }

        public MotiveVisual3D(StructureWrap motive, Color color)
        {
            _motive = motive;
            _material = new StructureMaterial(color);

            Structure = motive.Structure;
            BoundingSphereRadius = Structure.GeometricalCenterAndRadius().Radius;

            _model = new StructureModel3D(Structure);

            //var atoms = _model.Atoms.ToDictionary(a => a, a => new AtomVisual(a, this, _material));            
            //var bondLists = atoms.ToDictionary(a => a.Key, _ => new HashSet<BondVisual>());

            _atomVisuals = _model.Atoms.Select(a => new AtomVisual(a, this, _material)).ToArray();
            _bondVisuals = _model.Bonds.Select(b => new BondVisual(b, _material)).ToArray();
                ////{
                ////    var ret = new BondVisual(b, _material);
                ////    bondLists[b.A].Add(ret);
                ////    bondLists[b.B].Add(ret);
                ////    return ret;
                ////}).ToArray();
        }

        public void Attach(MultiMotiveVisual3D visual)
        {
            this.MultiMotiveVisual = visual;
            for (int i = 0; i < _atomVisuals.Length; i++)
            {
                _atomVisuals[i].RenderMode = visual.RenderMode;
                _atomVisuals[i].AtomDisplayMode = visual.AtomDisplayMode;
                _atomVisuals[i].ColorMode = visual.AtomColorMode;
            }
            for (int i = 0; i < _bondVisuals.Length; i++)
            {
                _bondVisuals[i].RenderMode = visual.RenderMode;
                _bondVisuals[i].AtomDisplayMode = visual.AtomDisplayMode;
            }
        }

        public void Detach(MultiMotiveVisual3D visual)
        {
            this.MultiMotiveVisual = null;
            for (int i = 0; i < _atomVisuals.Length; i++) _atomVisuals[i].RenderMode = RenderMode.Nothing;
            for (int i = 0; i < _bondVisuals.Length; i++) _bondVisuals[i].RenderMode = RenderMode.Nothing;
        }

        public void SetRenderMode(RenderMode mode)
        {
            _atomVisuals.ForEach(a => a.RenderMode = mode);
            _bondVisuals.ForEach(b => b.RenderMode = mode);
        }

        public void UpdateColorMode(AtomColorMode colorMode) 
        {
            for (int i = 0; i < _atomVisuals.Length; i++)
            {
                _atomVisuals[i].ColorMode = colorMode;
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < _atomVisuals.Length; i++)
            {
                _atomVisuals[i].Dispose();
            }
            for (int i = 0; i < _bondVisuals.Length; i++)
            {
                _bondVisuals[i].Dispose();
            }

            _atomVisuals = null;
            _bondVisuals = null;

            //_motive = null;
            _material = null;

            _model.Dispose();
        }

        public override void Render(RenderContext context)
        {
            if (MultiMotiveVisual.RenderMode == RenderMode.Nothing) return;

            _atomVisuals.ForEach(a => a.Render(context));
            _bondVisuals.ForEach(b => b.Render(context));
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            _model.UpdateAsync(args);
        }
    }
}
