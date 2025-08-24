namespace WebChemistry.Framework.Visualization.Visuals
{
    using WebChemistry.Framework.Core;
    using System.Windows.Media.Media3D;
    using System.Windows.Media;

    class AtomModel
    {
        Model3DGroup _model;
        public Model3DGroup Model
        {
            get
            {
                _model = _model ?? new Model3DGroup();
                return _model;
            }
        }
        public IAtom Atom { get; private set; }
        public PdbStructureVisual Structure { get; private set; }

        bool isHetAtom;
        bool isWater;

        Model3DGroup atomModel, vdwModel;
        Model3DGroup backboneModel;
        //GeometryModel3D selectionModel;

        DiffuseMaterial _diffuseMaterial;
        DiffuseMaterial DiffuseMaterial
        {
            get
            {
                _diffuseMaterial = _diffuseMaterial ?? new DiffuseMaterial(new SolidColorBrush(PdbStructureColoring.GetAtomColor(Structure, this)));
                return _diffuseMaterial;
            }
        }
        
        TranslateTransform3D _translationTransform;
        TranslateTransform3D TranslationTransform
        {
            get
            {
                _translationTransform = _translationTransform ?? new TranslateTransform3D(Atom.Position.X, Atom.Position.Y, Atom.Position.Z);
                return _translationTransform;
            }
        }

        private void CreateUnbondedSphere()
        {
            GeometryModel3D sphereModel = new GeometryModel3D();
            sphereModel.Geometry = Sphere.Get(Structure.SphereDivisions);
            sphereModel.Material = this.DiffuseMaterial;
            this.atomModel.Children.Add(sphereModel);

            Transform3DGroup sphereTransformGroup = new Transform3DGroup();
            sphereModel.Transform = sphereTransformGroup;

            var atomScale = new ScaleTransform3D(0.2, 0.2, 0.2);
            sphereTransformGroup.Children.Add(atomScale);
            sphereTransformGroup.Children.Add(TranslationTransform);

            sphereTransformGroup.Freeze();
        }

        //private void CreateSelectionSphere()
        //{
        //    this.selectionModel = new GeometryModel3D();
        //    this.selectionModel.Geometry = Sphere.Mesh;
        //    this.selectionModel.Material = this.diffuseMaterial;

        //    Transform3DGroup sphereTransformGroup = new Transform3DGroup();
        //    this.selectionModel.Transform = sphereTransformGroup;

        //    ScaleTransform3D scaleTransform = new ScaleTransform3D(0.4, 0.4, 0.4);
        //    sphereTransformGroup.Children.Add(scaleTransform);

        //    sphereTransformGroup.Children.Add(this.translationTransform);
        //}

        private void CreateVdwSphere()
        {
            GeometryModel3D sphereModel = new GeometryModel3D();
            sphereModel.Geometry = Sphere.Get(3);
            sphereModel.Material = this.DiffuseMaterial;
            this.vdwModel.Children.Add(sphereModel);

            Transform3DGroup sphereTransformGroup = new Transform3DGroup();
            sphereModel.Transform = sphereTransformGroup;

            var s = Atom.GetVdwRadius();
            var atomScale = new ScaleTransform3D(s, s, s);
            sphereTransformGroup.Children.Add(atomScale);
            sphereTransformGroup.Children.Add(TranslationTransform);

            sphereTransformGroup.Freeze();
        }

        protected void CreateBondStick(Model3DGroup modelGroup, WebChemistry.Framework.Math.Vector3D other)
        {
            var distance = WebChemistry.Framework.Math.MathEx.DistanceTo(Atom.Position, other);

            GeometryModel3D stickModel = new GeometryModel3D();
            stickModel.Geometry = distance > 2 ? Stick.GetLong(Structure.StickDivisions) : Stick.GetShort(Structure.StickDivisions);
            stickModel.Material = this.DiffuseMaterial;
            modelGroup.Children.Add(stickModel);

            Transform3DGroup transformGroup = new Transform3DGroup();
            stickModel.Transform = transformGroup;

            ScaleTransform3D scaleTransform = new ScaleTransform3D();
            scaleTransform.ScaleX = distance / 2;
            transformGroup.Children.Add(scaleTransform);
            
            RotateTransform3D rotationTransform = new RotateTransform3D();
            transformGroup.Children.Add(rotationTransform);

            Vector3D orientationVector = new Vector3D(1, 0, 0);
            Vector3D differenceVector = new Vector3D(other.X - this.Atom.Position.X,
                other.Y - this.Atom.Position.Y, other.Z - this.Atom.Position.Z);

            AxisAngleRotation3D rotation = new AxisAngleRotation3D();
            rotation.Angle = Vector3D.AngleBetween(orientationVector, differenceVector);
            rotation.Axis = Vector3D.CrossProduct(orientationVector, differenceVector);

            if (rotation.Axis.LengthSquared > 0)
                rotationTransform.Rotation = rotation;

            transformGroup.Children.Add(this.TranslationTransform);

            transformGroup.Freeze();
        }

        public void ShowVdwSphere(bool show)
        {
            if (show && this.vdwModel == null)
            {
                if (isWater && !Structure.ShowWaters) return;
                if (isHetAtom && !Structure.ShowHetAtoms) return;

                this.vdwModel = new Model3DGroup();
                this.CreateVdwSphere();
                this.Model.Children.Add(this.vdwModel);
            }
            else if (show && !this.Model.Children.Contains(this.vdwModel))
            {
                this.Model.Children.Add(this.vdwModel);
            }
            else if (!show && this.Model.Children.Contains(this.vdwModel))
            {
                this.Model.Children.Remove(this.vdwModel);
            }
        }

        public void ShowAtom(bool show)
        {
            if ((isHetAtom || isWater) && (Structure.Structure.Atoms.Count > PdbStructureVisual.MaxHetAtoms)) return;

            if (show && this.atomModel == null)
            {
                this.atomModel = new Model3DGroup();

                int numBonds = 0;
                foreach (var bond in Structure.Structure.Bonds[Atom])
                {
                    //if (bond.Type == BondType.Metallic) continue;
                    this.CreateBondStick(this.atomModel, bond.OtherAtom(Atom).Position);
                    numBonds++;
                }
                if (numBonds == 0) this.CreateUnbondedSphere();

                this.Model.Children.Add(this.atomModel);
            }
            else if (show && !this.Model.Children.Contains(this.atomModel))
            {
                this.Model.Children.Add(this.atomModel);
            }
            else if (!show && this.atomModel != null &&
                this.Model.Children.Contains(this.atomModel))
            {
                this.Model.Children.Remove(this.atomModel);
            }
        }

        public void ShowBackbone(bool show)
        {
            if (backboneModel == null) return;

            //if (show && this.Model == null)
            //{
            //    this.Model = new Model3DGroup();
            //}
            if (show && !this.Model.Children.Contains(this.backboneModel))
            {
                this.Model.Children.Add(this.backboneModel);
            }
            else if (!show && this.backboneModel != null &&
                this.Model.Children.Contains(this.backboneModel))
            {
                this.Model.Children.Remove(this.backboneModel);
            }
        }

        internal void CreateBackbonePart(IAtom other)
        {
            if (this.backboneModel == null) this.backboneModel = new Model3DGroup();
            this.CreateBondStick(this.backboneModel, other.Position);
        }
        
        public void UpdateColor()
        {
            if (Atom.IsHighlighted)
            {
                ((SolidColorBrush)this.DiffuseMaterial.Brush).Color = PdbStructureColoring.HighlightColor;
            }
            else if (Atom.IsSelected)
            {
                ((SolidColorBrush)this.DiffuseMaterial.Brush).Color = PdbStructureColoring.GetAtomColor(Structure, this, PdbStructureColorScheme.Atom);// PdbStructureColoring.HighlightColor;
            }
            else
            {
                ((SolidColorBrush)this.DiffuseMaterial.Brush).Color = PdbStructureColoring.GetAtomColor(Structure, this);
            }
        }

        void Update()
        {
            if (Structure.DisplayType == PdbStructureDisplayType.Backbone)
            {
                UpdateColor();
                return;
            }

            UpdateColor();

            if (Atom.IsHighlighted && Atom.IsSelected)
            {
                ShowAtom(true);
            }
            else if (!Atom.IsHighlighted && Atom.IsSelected)
            {
                ShowAtom(true);
            }
            else if (Atom.IsHighlighted && !Atom.IsSelected)
            {
                ShowAtom(isHetAtom || isWater || Structure.DisplayType == PdbStructureDisplayType.FullChain);
            }
            else
            {
                ShowAtom((Structure.ShowHetAtoms && isHetAtom && !isWater) || (Structure.ShowWaters && isWater) || Structure.DisplayType == PdbStructureDisplayType.FullChain);
            }
        }

        public static AtomModel Create(IAtom atom, PdbStructureVisual structure)
        {
            AtomModel ret = new AtomModel(atom);
            ret.Structure = structure;
            ret.isHetAtom = atom.IsHetAtom();
            ret.isWater = atom.IsWater();

            atom.ObserveInteractivePropertyChanged(ret, (l, _) => l.Update());

            //ret.DiffuseMaterial = new DiffuseMaterial(new SolidColorBrush(PdbStructureColoring.GetAtomColor(structure, ret)));
            //ret.TranslationTransform = new TranslateTransform3D(atom.Position.X, atom.Position.Y, atom.Position.Z);
            //ret.Model = new Model3DGroup();
            //ret.CreateSelectionSphere();
            
            return ret;
        }

        public bool HitTest(RayMeshGeometry3DHitTestResult ray)
        {
            if (Structure.DisplayType == PdbStructureDisplayType.Backbone)
            {
                if (this.backboneModel != null && this.backboneModel.Children.Contains(ray.ModelHit)) return true;
                if ((isWater || isHetAtom) && this.atomModel != null && this.atomModel.Children.Contains(ray.ModelHit)) return true;
            }
            else if (this.atomModel != null && this.atomModel.Children.Contains(ray.ModelHit)) return true;

            return false;
        }

        protected AtomModel(IAtom atom)
        {
            this.Atom = atom;
        }
    }
}
