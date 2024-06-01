using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using System.Windows.Media;

namespace WebChemistry.SiteBinder.Silverlight.Visuals
{
    public class AtomVisual : VisualElement3D
    {
        Ellipse baseVisual, overlayVisual;
        AtomModel3D model;
        MotiveVisual3D motiveVisual;
        StructureMaterial material;
        RenderMode renderMode;
        AtomDisplayMode atomDisplayMode;
        AtomColorMode colorMode;

        public AtomDisplayMode AtomDisplayMode
        {
            set { atomDisplayMode = value; UpdateVisibility(); }
        }

        public AtomColorMode ColorMode
        {
            set { colorMode = value; UpdateMaterial(); }
        }

        public RenderMode RenderMode
        {
            get { return renderMode; }
            set
            {
                renderMode = value;

                UpdateRenderMode();
                UpdateMaterial();
            }
        }

        void UpdateRenderMode()
        {
            if (renderMode == SiteBinder.RenderMode.BallsAndSticks || renderMode == SiteBinder.RenderMode.Sticks)
            {
                if (baseVisual == null) baseVisual = VisualsManager<Ellipse>.Withdraw();
                if (overlayVisual == null) overlayVisual = VisualsManager<Ellipse>.Withdraw();

                model.Radius = renderMode == SiteBinder.RenderMode.BallsAndSticks ? 0.17 : 0.11;

                baseVisual.StrokeThickness = 0;
                baseVisual.IsHitTestVisible = false;

                overlayVisual.StrokeThickness = 0;
                overlayVisual.Tag = model.Atom;
                overlayVisual.IsHitTestVisible = true;
                overlayVisual.Fill = AtomMaterials.OverlayBrush;
            }
            else if (renderMode == SiteBinder.RenderMode.Wireframe)
            {
                VisualsManager<Ellipse>.Deposit(overlayVisual);
                overlayVisual = null;

                if (baseVisual == null) baseVisual = VisualsManager<Ellipse>.Withdraw();

                model.Radius = 0.09;

                baseVisual.StrokeThickness = 0;
                baseVisual.IsHitTestVisible = true;
                baseVisual.Tag = model.Atom;
            }
            else // Nothing
            {
                VisualsManager<Ellipse>.Deposit(baseVisual);
                VisualsManager<Ellipse>.Deposit(overlayVisual);
                baseVisual = null;
                overlayVisual = null;
            }
        }

        public AtomVisual(AtomModel3D model, MotiveVisual3D mv, StructureMaterial material)
        {
            model.Radius = 0.17;
            this.material = material;           
            this.motiveVisual = mv;
            this.model = model;
            model.Atom.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Atom_PropertyChanged);
        }
        
        public void UpdateVisibility()
        {
            bool visible = true;

            if (atomDisplayMode == AtomDisplayMode.Selection) visible = model.Atom.IsSelected;

            if (visible)
            {
                if (baseVisual != null) baseVisual.Visibility = System.Windows.Visibility.Visible;
                if (overlayVisual != null) overlayVisual.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if (baseVisual != null) baseVisual.Visibility = System.Windows.Visibility.Collapsed;
                if (overlayVisual != null) overlayVisual.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public Brush GetAtomMaterial(IAtom atom, System.Windows.Media.Brush structureMaterial)
        {
            if (renderMode == SiteBinder.RenderMode.BallsAndSticks || renderMode == SiteBinder.RenderMode.Wireframe)
            {
                if (colorMode == SiteBinder.AtomColorMode.Element) return AtomMaterials.GetAtomMaterial(atom).Brush;
                else return atom.IsSelected ? AtomMaterials.SelectedMaterial.Brush : AtomMaterials.StandardMaterial.Brush;
            }
            else
            {
                return atom.IsSelected ? AtomMaterials.SelectedMaterial.Brush : structureMaterial;
            }
        }

        public void UpdateMaterial()
        {
            if (baseVisual == null) return;

            var brush = GetAtomMaterial(this.model.Atom, this.material.Brush);
            baseVisual.Fill = brush;
        }

        void Atom_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (disposed || renderMode == SiteBinder.RenderMode.Nothing) return;

            if (e.PropertyName.Equals("IsSelected", StringComparison.Ordinal))
            {
                UpdateVisibility();
                UpdateMaterial();
            }
            else if (e.PropertyName.Equals("IsHighlighted", StringComparison.Ordinal))
            {
                if (motiveVisual.MultiMotiveVisual == null) return;

                if ((sender as IAtom).IsHighlighted)
                {
                    motiveVisual.MultiMotiveVisual.ShowToolip(model, motiveVisual.Structure);
                    baseVisual.Fill = AtomMaterials.HighlightedMaterial.Brush;
                }
                else
                {
                    motiveVisual.MultiMotiveVisual.HideTooltip();
                    UpdateMaterial();
                }
            }
        }
        
        public override void Render(RenderContext context)
        {
            var boundingBox = model.BoundingBox;

            int z = model.ZIndex;
            if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;

            baseVisual.Width = boundingBox.Width;
            baseVisual.Height = boundingBox.Height;
            baseVisual.SetValue(Canvas.LeftProperty, boundingBox.Left);
            baseVisual.SetValue(Canvas.TopProperty, boundingBox.Top);
            baseVisual.SetValue(Canvas.ZIndexProperty, z);

            if (overlayVisual != null)
            {
                overlayVisual.Width = boundingBox.Width;
                overlayVisual.Height = boundingBox.Height;
                overlayVisual.SetValue(Canvas.LeftProperty, boundingBox.Left);
                overlayVisual.SetValue(Canvas.TopProperty, boundingBox.Top);
                overlayVisual.SetValue(Canvas.ZIndexProperty, z + 1);
            }
        }

        public override void Register(Viewport3DBase viewport)
        {
            if (baseVisual != null) viewport.Canvas.Children.Add(baseVisual);
            if (overlayVisual != null) viewport.Canvas.Children.Add(overlayVisual);
        }

        bool disposed = false;
        public override void Dispose()
        {
            disposed = true;

            model.Atom.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Atom_PropertyChanged);
            VisualsManager<Ellipse>.Deposit(baseVisual);
            VisualsManager<Ellipse>.Deposit(overlayVisual);

            baseVisual = null;
            overlayVisual = null;
            model.Dispose();
            model = null;
        }
    }
}
