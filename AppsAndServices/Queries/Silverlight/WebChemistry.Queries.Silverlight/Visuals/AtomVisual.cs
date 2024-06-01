using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using System.Windows.Media;

namespace WebChemistry.Queries.Silverlight.Visuals
{
    public class AtomVisual : VisualElement3D
    {
        Ellipse baseVisual;
        AtomModel3D model;
        MotiveVisual3D parent;
        
        void Update()
        {
            if (baseVisual == null) baseVisual = VisualsManager<Ellipse>.Withdraw();
            baseVisual.StrokeThickness = 0;
            baseVisual.IsHitTestVisible = true;
            baseVisual.Visibility = System.Windows.Visibility.Visible;
            baseVisual.Tag = model.Atom;

            if (model.Atom.IsSelected)
            {
                model.Radius = 0.14;
            }
            else
            {
                model.Radius = 0.08;
            }

            UpdateMaterial();
        }

        void UpdateMaterial()
        {
            if (model.Atom.IsSelected)
            {
                baseVisual.Fill = AtomMaterials.GetAtomMaterial(model.Atom).Brush;
            }
            else
            {
                baseVisual.Fill = BackgroundMaterial.Brush; // AtomMaterials.StandardMaterial.Brush;
            }
        }

        public AtomVisual(AtomModel3D model, MotiveVisual3D parent)
        {
            this.parent = parent;
            this.model = model;
            model.Atom.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Atom_PropertyChanged);

            this.Update();
        }
        
        void Atom_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (disposed || baseVisual == null) return; 

            if (e.PropertyName.Equals("IsHighlighted", StringComparison.Ordinal))
            {
                if (parent.Parent == null) return;

                if ((sender as IAtom).IsHighlighted)
                {
                    parent.Parent.ShowToolip(model);
                    baseVisual.Fill = AtomMaterials.HighlightedMaterial.Brush;
                }
                else
                {
                    parent.Parent.HideTooltip();
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
        }

        public override void Register(Viewport3DBase viewport)
        {
            if (baseVisual != null) viewport.Canvas.Children.Add(baseVisual);
        }

        bool disposed = false;
        public override void Dispose()
        {
            disposed = true;

            model.Atom.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Atom_PropertyChanged);
            VisualsManager<Ellipse>.Deposit(baseVisual);

            baseVisual = null;
            model = null;
        }
    }
}
