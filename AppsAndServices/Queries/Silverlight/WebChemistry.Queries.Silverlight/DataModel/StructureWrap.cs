using System.IO;
using System.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Silverlight.Visuals;
using WebChemistry.Framework.Visualization;
using System.Windows.Media;
using WebChemistry.Silverlight.Common.DataModel;

namespace WebChemistry.Queries.Silverlight.DataModel
{
    public class StructureWrap : StructureWrapBase<StructureWrap> , IVisual
    {
        int _motiveCount;
        public int MotiveCount
        {
            get { return _motiveCount; }
            set
            {
                if (_motiveCount == value) return;
                _motiveCount = value;
                NotifyPropertyChanged("MotiveCount");
            }
        }
        
        public override string ToString()
        {
            return Structure.Id;
        }

        MotiveVisual3D visual;
        StructureModel3D model;

        public MotiveVisual3D Visual
        {
            get 
            {
                return visual = visual ?? new MotiveVisual3D(model, this.Structure, Color.FromArgb(255, 0x33, 0x33, 0x33));
            }
        }

        public void ClearVisual()
        {
            if (visual != null)
            {
                visual.Dispose();
                visual = null;
            }
        }

        public Framework.Geometry.GeometricalCenterInfo CenterInfo { get; private set; }


        protected override bool SelectAtomsInternal(System.Collections.Generic.IEnumerable<IAtom> atoms, bool selected)
        {
            bool changed = false;
            foreach (var a in atoms)
            {
                if (a.IsSelected != selected)
                {
                    changed = true;
                    a.IsSelected = selected;
                }
            }
            return changed;
        }

        public override void Dispose()
        {
            ClearVisual();
        }

        protected override void OnCreate()
        {
            Structure.ToCentroidCoordinates();
            model = new StructureModel3D(Structure);
            CenterInfo = Structure.GeometricalCenterAndRadius();
        }
    }
}
