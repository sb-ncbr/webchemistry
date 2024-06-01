using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using WebChemistry.Queries.Silverlight.DataModel;

namespace WebChemistry.Queries.Silverlight.Visuals
{
    public class MotiveVisual3D : Visual3D
    {
        AtomVisual[] atomVisuals;
        BondVisual[] bondVisuals;
        StructureMaterial material;
        StructureModel3D model;
        
        public MotiveVisual3DWrap Parent { get; private set; }
                
        //public Color Color { get { return _material.Color; } set { _material.Color = value; UpdateColorMode(MultiMotiveVisual.AtomColorMode); } }
                
        public override void Register(Viewport3DBase vp)
        {
            base.Register(vp);

            atomVisuals.ForEach(a => a.Register(vp));
            bondVisuals.ForEach(a => a.Register(vp));
        }

        public MotiveVisual3D(StructureModel3D model, IStructure structure, Color color)
        {
            material = new StructureMaterial(color);

            BoundingSphereRadius = 1;// Structure.GeometricalCenterAndRadius().Radius;

            this.model = model;

            var atoms = model.Atoms.ToDictionary(a => a, a => new AtomVisual(a, this));            
            var bondLists = atoms.ToDictionary(a => a.Key, _ => new HashSet<BondVisual>());

            atomVisuals = atoms.Values.ToArray();
            bondVisuals = model.Bonds.Select(b =>
                {
                    var ret = new BondVisual(b, material);
                    bondLists[b.A].Add(ret);
                    bondLists[b.B].Add(ret);
                    return ret;
                }).ToArray();
        }

        public void Attach(MotiveVisual3DWrap visual)
        {
            this.Parent = visual;
        }

        public void Detach(MotiveVisual3DWrap visual)
        {
            this.Parent = null;
        }

        public override void Dispose()
        {
            for (int i = 0; i < atomVisuals.Length; i++)
            {
                atomVisuals[i].Dispose();
            }
            for (int i = 0; i < bondVisuals.Length; i++)
            {
                bondVisuals[i].Dispose();
            }

            atomVisuals = null;
            bondVisuals = null;

            //_motive = null;
            material = null;
        }

        public override void Render(RenderContext context)
        {
            atomVisuals.ForEach(a => a.Render(context));
            bondVisuals.ForEach(b => b.Render(context));
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            model.UpdateAsync(args);
        }
    }
}
