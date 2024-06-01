using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using WebChemistry.SiteBinder.Silverlight.ViewModel;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using Microsoft.Practices.ServiceLocation;


namespace WebChemistry.SiteBinder.Silverlight.Visuals
{
    public class MultiMotiveVisual3D : Visual3D
    {
        FrameworkElement tooltipVisual;
        RenderMode renderMode = RenderMode.Wireframe;
        AtomColorMode atomColorMode = AtomColorMode.Structure;
        AtomDisplayMode atomDisplayMode = AtomDisplayMode.All;

        class MotiveVisualComparer : IEqualityComparer<MotiveVisual3D>
        {
            public bool Equals(MotiveVisual3D x, MotiveVisual3D y)
            {
                return x.Structure.Id.EqualOrdinalIgnoreCase(y.Structure.Id); //x.Motive.Name == y.Motive.Name;
            }

            public int GetHashCode(MotiveVisual3D obj)
            {
                return obj.Structure.Id.GetHashCode();
            }
        }
                
        int _maxVisuals = 25;

        public RenderMode RenderMode
        {
            get {  return renderMode;  }
            set
            {
                if (renderMode == value) return;

                renderMode = value;

                Viewport.CancelRender();
                Viewport.SuspendRender = true;

                Viewport.Canvas.Children.Clear();
                Viewport.Canvas.Children.Add(tooltipVisual);
                _visuals.ForEach(v => 
                    {
                        v.SetRenderMode(value);
                        v.Register(_viewport);
                    });

                Viewport.SuspendRender = false;
                Viewport.Render();
            }
        }

        public AtomColorMode AtomColorMode
        {
            get { return atomColorMode; }
            set
            {
                atomColorMode = value;
                _visuals.ForEach(v => v.UpdateColorMode(value));
            }
        }

        public AtomDisplayMode AtomDisplayMode
        {
            get { return atomDisplayMode; }
            set
            {
                atomDisplayMode = value;
                _visuals.ForEach(v => v.AtomDisplayMode = value);
            }
        }

        public int MaxVisuals
        {
            get { return _maxVisuals; }
            set
            {
                if (_maxVisuals == value) return;
                _maxVisuals = value;
                UpdateMaxVisualsCount();
            }
        }

        public bool SuppressTooltip { get; set; }

        public class TooltipInfo
        {
            public string Name { get; set; }
            public int SerialNumber { get; set; }
            public string Residue { get; set; }
            public int ResidueSequenceNumber { get; set; }
            public string ChainIdentifier { get; set; }
            public string Structure { get; set; }
        }

        internal void ShowToolip(AtomModel3D atom, IStructure motive)
        {
            if (SuppressTooltip) return;

            tooltipVisual.DataContext =
                new TooltipInfo
                { 
                    Name = atom.Atom.PdbName(), 
                    SerialNumber = atom.Atom.PdbSerialNumber(), 
                    Residue = atom.Atom.PdbResidueName(), 
                    ResidueSequenceNumber = atom.Atom.PdbResidueSequenceNumber(),
                    ChainIdentifier = atom.Atom.PdbChainIdentifier().ToString(),
                    Structure = motive.Id
                };
            tooltipVisual.Visibility = System.Windows.Visibility.Visible;

            Canvas.SetZIndex(tooltipVisual, Int16.MaxValue - 1);
            Canvas.SetLeft(tooltipVisual, atom.BoundingBox.Right + 7);
            Canvas.SetTop(tooltipVisual, atom.BoundingBox.Top + atom.BoundingBox.Height / 2);
        }

        internal void HideTooltip()
        {
            tooltipVisual.Visibility = System.Windows.Visibility.Collapsed;
        }

        HashSet<MotiveVisual3D> _visuals = new HashSet<MotiveVisual3D>(new MotiveVisualComparer());


        void SetCameraRadius(IEnumerable<StructureWrap> newMotives)
        {
            if (newMotives.Count() == 0) return;

            double currentRadius = Viewport.Camera.Radius;

            var radius = newMotives.Max(m => m.CenterInfo.Radius);
            if (radius < 10) radius *= 4;
            else if (radius < 50) radius *= 3.2;
            else if (radius < 100) radius *= 2.7;
            else radius *= 2.5;

            if (radius < 1) radius = 1;
            else if (radius > 155) radius = 155;

            if (radius > currentRadius)
            {
                Viewport.Camera.Radius = radius;
            }
        }

        public void ShowSelection(bool show = true)
        {
            throw new NotImplementedException();
        }

        public void AddOffscreen(IEnumerable<StructureWrap> motives)
        {
            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            var toAdd = motives.ToArray();

            toAdd.ForEach(motive =>
            {
                var visual = motive.Visual;
                visual.Attach(this);
                _visuals.Add(visual);
                visual.Register(_viewport);
            });
            
            Viewport.SuspendRender = false;
        }
        
        void UpdateMaxVisualsCount()
        {
            RemoveAll();
            Add(ServiceLocator.Current.GetInstance<Session>().SelectedStructures);
        }

        public void Add(IEnumerable<StructureWrap> motives)
        {
            if (_visuals.Count >= _maxVisuals) return;

            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            var toAdd = motives.Take(_maxVisuals).ToArray();

            toAdd.ForEach(motive =>
                {
                    var visual = motive.Visual;
                    visual.Attach(this);
                    _visuals.Add(visual);
                    visual.Register(_viewport);
                });

            SetCameraRadius(toAdd);

            Viewport.SuspendRender = false;
            Viewport.Render();
        }

        public void RemoveAll()
        {
            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            var toClear = _visuals.Select(m => m.Motive).ToArray();
            _visuals.Clear();
            Viewport.Canvas.Children.Clear();

            toClear.ForEach(m => m.ClearVisual());

            Viewport.Canvas.Children.Add(tooltipVisual);

            VisualsManager<BondPath3D>.Free();
            VisualsManager<System.Windows.Shapes.Ellipse>.Free();

            Viewport.SuspendRender = false;
            Viewport.Render();
        }

        public void Remove(IEnumerable<StructureWrap> xs, IEnumerable<StructureWrap> allSelected)
        {
            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            Viewport.Canvas.Children.Clear();
            foreach (var s in xs)
            {
                if (s.HasVisual) _visuals.Remove(s.Visual);
                s.ClearVisual();
            }
            Register(Viewport);


            var neededMotives = MaxVisuals - _visuals.Count;
            var visualized = _visuals.Select(v => v.Structure.Id).ToHashSet();
            var toAdd = allSelected.Where(s => !visualized.Contains(s.Structure.Id)).Take(neededMotives).ToArray();
            if (toAdd.Length > 0) Add(toAdd);


            Viewport.SuspendRender = false;
            Viewport.Render();
        }

        public override void Register(Viewport3DBase viewport)
        {
            base.Register(viewport);
            viewport.Canvas.Children.Add(tooltipVisual);
            _visuals.ForEach(v => v.Register(Viewport));
        }

        public override void Render(RenderContext context)
        {
            _visuals.ForEach(v => v.Render(context));
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            _visuals.ForEach(v => v.UpdateAsync(args));
        }

        public void Render()
        {
            Viewport.CancelRender();
            Viewport.Render();
        }

        public System.Windows.Media.Brush GetAtomMaterial(IAtom atom, System.Windows.Media.Brush structureMaterial)
        {
            if (renderMode == SiteBinder.RenderMode.BallsAndSticks)
            {
                if (atomColorMode == SiteBinder.AtomColorMode.Element) return AtomMaterials.GetAtomMaterial(atom).Brush;
                else return atom.IsSelected ? AtomMaterials.SelectedMaterial.Brush : AtomMaterials.StandardMaterial.Brush;
            }
            else
            {
                return atom.IsSelected ? AtomMaterials.SelectedMaterial.Brush : structureMaterial;
            }
        }

        public override void Dispose()
        {

        }

        public MultiMotiveVisual3D()
        {
            SuppressTooltip = true;

            tooltipVisual = (Application.Current.Resources["VisualTooltipTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
            tooltipVisual.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
