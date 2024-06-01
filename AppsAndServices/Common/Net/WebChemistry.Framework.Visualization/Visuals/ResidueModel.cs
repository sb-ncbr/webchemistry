namespace WebChemistry.Framework.Visualization.Visuals
{
    using WebChemistry.Framework.Core;
    using System.Windows.Media.Media3D;
    using System.Windows.Media;
    using System.Linq;
    using WebChemistry.Framework.Core.Pdb;

    class ResidueModel
    {
        PdbStructureVisual structureVisual;

        public PdbResidue Residue { get; private set; }
        public Ribbon Ribbon { get; set; }
        public Cartoon Cartoon { get; set; }
        public Model3DGroup Model { get; private set; }

        public bool IsStructureStart { get; private set; }
        public bool IsStructureEnd { get; private set; }

        public void UpdateColor()
        {
            if (Cartoon != null)
            {
                Cartoon.Color = PdbStructureColoring.GetResidueColor(structureVisual, Residue);
            }
        }

        void Update()
        {
            if (Cartoon != null)
            {
                if (Residue.IsHighlighted)
                {
                    Cartoon.Color = PdbStructureColoring.HighlightColor;
                }
                else if (Residue.IsSelected)
                {
                    Cartoon.Color = PdbStructureColoring.SelectColor;
                }
                else
                {
                    UpdateColor();
                }
            }            
        }

        public void ShowCartoon(bool show)
        {
            if (this.Ribbon != null)
            {
                if (show && this.Cartoon == null)
                {
                    Color color = PdbStructureColoring.GetResidueColor(structureVisual, Residue);
                    this.Cartoon = new Cartoon(this, color, structureVisual.RadialSegmentCount);
                }

                if (show && !this.Model.Children.Contains(this.Cartoon.Model))
                {
                    this.Model.Children.Add(this.Cartoon.Model);
                }
                else if (!show && this.Model.Children.Contains(this.Cartoon.Model))
                {
                    this.Model.Children.Remove(this.Cartoon.Model);
                }
            }
        }

        public static ResidueModel Create(PdbResidue r, PdbStructureVisual structureVisual)
        {
            var ret = new ResidueModel(r);
            ret.structureVisual = structureVisual;
            ret.Model = new Model3DGroup();

            var helices = structureVisual.Structure.PdbHelices();
            var sheets = structureVisual.Structure.PdbSheets();

            ret.IsStructureStart =
                helices.Any(h => h.Residues[0].Equals(r))
                || sheets.Any(s => s.Residues[0].Equals(r));


            ret.IsStructureEnd =
                helices.Any(h => h.Residues[h.Residues.Count - 1].Equals(r))
                || sheets.Any(s => s.Residues[s.Residues.Count - 1].Equals(r));
            
            r.ObserveInteractivePropertyChanged(ret, (t, _) => t.Update());

            return ret;
        }

        private ResidueModel(PdbResidue r)
        {
            this.Residue = r;
        }

        public bool HitTest(RayMeshGeometry3DHitTestResult ray)
        {
            if (Cartoon != null) return Cartoon.HoverHitTest(ray);
            return false;
        }
    }
}
