namespace WebChemistry.Framework.Visualization.Visuals
{
    using System.Windows;
    using System.Windows.Media.Media3D;
    using WebChemistry.Framework.Core;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using System.Windows.Media;
    using System.Windows.Input;

    public class PdbStructureVisual : ModelVisual3D, IInteractiveVisual
    {
        public bool ShowHetAtoms
        {
            get { return (bool)GetValue(ShowHetAtomsProperty); }
            set { SetValue(ShowHetAtomsProperty, value); }
        }

        public static readonly DependencyProperty ShowHetAtomsProperty =
            DependencyProperty.Register("ShowHetAtoms", typeof(bool), typeof(PdbStructureVisual), new PropertyMetadata(false, ShowHetAtomsChanged));

        private static void ShowHetAtomsChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as PdbStructureVisual).DisplayHetAtoms();
        }

        public bool ShowWaters
        {
            get { return (bool)GetValue(ShowWatersProperty); }
            set { SetValue(ShowWatersProperty, value); }
        }

        public static readonly DependencyProperty ShowWatersProperty =
            DependencyProperty.Register("ShowWaters", typeof(bool), typeof(PdbStructureVisual), new PropertyMetadata(false, ShowWatersChanged));

        private static void ShowWatersChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as PdbStructureVisual).DisplayWaters();
        }

        public PdbStructureDisplayType DisplayType
        {
            get { return (PdbStructureDisplayType)GetValue(DisplayTypeProperty); }
            set { SetValue(DisplayTypeProperty, value); }
        }

        public static readonly DependencyProperty DisplayTypeProperty =
            DependencyProperty.Register("DisplayType", typeof(PdbStructureDisplayType), typeof(PdbStructureVisual), new PropertyMetadata(PdbStructureDisplayType.Cartoon, DisplayTypeChanged));

        private static void DisplayTypeChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as PdbStructureVisual).UpdateDisplay();
        }

        public PdbStructureColorScheme ColorScheme
        {
            get { return (PdbStructureColorScheme)GetValue(ColorSchemeProperty); }
            set { SetValue(ColorSchemeProperty, value); }
        }

        public static readonly DependencyProperty ColorSchemeProperty =
            DependencyProperty.Register("ColorScheme", typeof(PdbStructureColorScheme), typeof(PdbStructureVisual), new PropertyMetadata(PdbStructureColorScheme.Background, ColorSchemeChanged));

        private static void ColorSchemeChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as PdbStructureVisual).UpdateColorScheme();
        }

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(PdbStructureVisual), new PropertyMetadata(Colors.DarkGray, BackgroundColorChanged));

        private static void BackgroundColorChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as PdbStructureVisual).UpdateColorScheme();
        }


        public IStructure Structure { get; private set; }
        
        Model3DGroup model;
        public Model3DGroup Model { get { return model; } }

        Ribbon[] ribbons;
        AtomModel[] atoms, chainAtoms, waterAtoms, hetAtoms;
        ResidueModel[] residues;
        
        void UpdateDisplay()
        {
            switch (DisplayType)
            {
                case PdbStructureDisplayType.Cartoon:
                    ShowBackbone(false);
                    ShowFull(false);
                    ShowVdwSpheres(false);
                    ShowCartoon(true);
                    break;
                case PdbStructureDisplayType.Backbone:
                    ShowCartoon(false);
                    ShowFull(false);
                    ShowVdwSpheres(false);
                    ShowBackbone(true);
                    break;
                case PdbStructureDisplayType.FullChain:
                    ShowCartoon(false);
                    ShowBackbone(false);
                    ShowVdwSpheres(false);
                    ShowFull(true);
                    break;
                case PdbStructureDisplayType.VdwSpheres:
                    ShowCartoon(false);
                    ShowBackbone(false);
                    ShowFull(false);
                    ShowVdwSpheres(true);
                    break;
                default:
                    break;
            }
        }

        void UpdateColorScheme()
        {
            residues.ForEach(r => r.UpdateColor());
            atoms.ForEach(a => a.UpdateColor());
        }

        void DisplayWaters()
        {
            if (Structure.Atoms.Count > maxAtoms)
            {
                ShowWaters = false;
                return;
            }
            waterAtoms.ForEach(a => a.ShowAtom(ShowWaters));
        }

        void DisplayHetAtoms()
        {
            if (Structure.Atoms.Count > MaxHetAtoms)
            {
                ShowHetAtoms = false;
                return;
            }
            hetAtoms.ForEach(a => a.ShowAtom(ShowHetAtoms));
        }

        void ShowBackbone(bool show)
        {
            atoms.ForEach(a => a.ShowBackbone(show));
        }

        void ShowCartoon(bool show)
        {
            if (Structure.Atoms.Count > maxAtoms) return;
            residues.ForEach(r => r.ShowCartoon(show));
        }

        void ShowFull(bool show)
        {
            if (Structure.Atoms.Count > maxAtoms)
            { 
                return;
            }
            chainAtoms.ForEach(a => a.ShowAtom(show));
        }

        void ShowVdwSpheres(bool show)
        {
            atoms.ForEach(a => a.ShowVdwSphere(show));
        }

        internal int LinearSegmentCount { get; private set; }
        internal int RadialSegmentCount { get; private set; }
        internal int SphereDivisions { get; private set; }
        internal int StickDivisions { get; private set; }

        internal const int MaxHetAtoms = 25000;
        const int maxAtoms = 150000;

        public static PdbStructureVisual Create(IStructure structure)
        {
            if (!structure.IsPdbStructure()) throw new ArgumentException("'structure' must be a PDB structure.");

            var ret = new PdbStructureVisual(structure);

            if (structure.Atoms.Count > 25000)
            {
                ret.LinearSegmentCount = 2;
                ret.RadialSegmentCount = 2;
                ret.SphereDivisions = 1;
                ret.StickDivisions = 4;
            }
            else
            {
                ret.LinearSegmentCount = 5;
                ret.RadialSegmentCount = 7;
                ret.SphereDivisions = 2;
                ret.StickDivisions = 6;
            }

            ret.atoms = structure.Atoms.Select(a => AtomModel.Create(a, ret)).ToArray();
            ret.residues = structure.PdbResidues().Select(r => ResidueModel.Create(r, ret)).ToArray();

            if (structure.Atoms.Count < maxAtoms) ret.CreateRibbons();           
 
            var dict = ret.atoms.ToDictionary(a => a.Atom, a => a);

            ret.waterAtoms = structure.PdbResidues().Where(r => r.IsWater).SelectMany(r => r.Atoms).Select(a => dict[a]).ToArray();
            ret.hetAtoms = structure.Atoms.Where(a => a.IsHetAtom() && !a.IsWater()).Select(a => dict[a]).ToArray();
            ret.chainAtoms = structure.PdbResidues().Where(r => r.IsAmino).SelectMany(r => r.Atoms).Select(a => dict[a]).ToArray();

            //ret.hetAtoms = structure.PdbHetAtoms().Select(a => dict[a]).ToArray();
            //ret.waterAtoms = structure.PdbWaterAtoms().Select(a => dict[a]).ToArray();
            //ret.chainAtoms = structure.PdbChainAtoms().Select(a => dict[a]).ToArray();

            foreach (var bond in structure.PdbBackbone().ProteinBackbone.Bonds)
            {
                dict[bond.A].CreateBackbonePart(bond.B);
                dict[bond.B].CreateBackbonePart(bond.A);
            }

            foreach (var bond in structure.PdbBackbone().DnaBackbone.Bonds)
            {
                dict[bond.A].CreateBackbonePart(bond.B);
                dict[bond.B].CreateBackbonePart(bond.A);
            }

            ret.CreateModel();
            if (structure.Atoms.Count <= maxAtoms) ret.ShowCartoon(true);
            else
            {
                ret.ShowBackbone(true);
                ret.ShowHetAtoms = false;
                ret.ShowWaters = false;
            }
            ret.Visual3DModel = ret.Model;
            ret.ShowHetAtoms = structure.Atoms.Count < MaxHetAtoms;

            return ret;
        }

        private void CreateRibbons()
        {
            var ribbons = new List<Ribbon>();
            Ribbon currentRibbon = null;
            ResidueModel previousResidue = null;
            foreach (var residue in this.residues)
            {
                if (residue.Residue.GetCAlpha() == null)
                {
                    currentRibbon = null;
                }
                else
                {
                    if (currentRibbon == null ||
                        residue.Residue.ChainIdentifier != previousResidue.Residue.ChainIdentifier)
                    {
                        currentRibbon = new Ribbon(this.LinearSegmentCount);
                        ribbons.Add(currentRibbon);
                    }

                    residue.Ribbon = currentRibbon;
                    currentRibbon.Residues.Add(residue);

                    previousResidue = residue;
                }
            }
                       

            foreach (Ribbon ribbon in ribbons)
                ribbon.CreateControlPoints();
            
            this.ribbons = ribbons.ToArray();
        }

        private void CreateModel()
        {
            this.model = new Model3DGroup();
            //this.model.Transform = this.moleculeTransformGroup;

            foreach (var atom in this.atoms)
            {
                this.model.Children.Add(atom.Model);
            }

            foreach (var residue in this.residues)
                this.model.Children.Add(residue.Model);
        }

        private PdbStructureVisual(IStructure structure)
        {
            this.Structure = structure;
        }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            if (ray.VisualHit != this) return null;

            for (int i = 0; i < atoms.Length; i++)
            {
                var a = atoms[i];
                if (a.HitTest(ray))
                {
                    return Structure.PdbResidues().FromAtom(a.Atom);
                }
            }

            for (int i = 0; i < residues.Length; i++)
            {
                var r = residues[i];
                if (r.HitTest(ray)) return r.Residue;
            }

            return null;
        }

        public bool IsHitTestVisible
        {
            get { return true; }
        }

        Key[] activationKeys = new System.Windows.Input.Key[0];
        public Key[] ActivationKeys
        {
            get { return activationKeys; }
        }
    }
}
