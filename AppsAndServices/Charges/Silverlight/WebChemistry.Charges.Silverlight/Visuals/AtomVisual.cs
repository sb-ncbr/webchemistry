using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using System.Windows.Media;

namespace WebChemistry.Charges.Silverlight.Visuals
{
    public class AtomVisual : VisualElement3D
    {
        Ellipse baseVisual;
        AtomModel3D model;
        ChargeStructureVisual3D chargeVisual;
        Brush material;

        public IAtom Atom { get { return model.Atom; } }

        public AtomVisual(AtomModel3D model, ChargeStructureVisual3D chargeVisual)
        {
            model.Radius = 0.22;
            this.model = model;
            this.chargeVisual = chargeVisual;

            baseVisual = VisualsManager<Ellipse>.Withdraw();
            baseVisual.StrokeThickness = 0;
            baseVisual.IsHitTestVisible = true;
            baseVisual.Visibility = System.Windows.Visibility.Visible;
            baseVisual.Tag = model.Atom;

            model.Atom.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Atom_PropertyChanged);
        }

        const double smallRadius = 0.25;
        const double bigRadius = 0.65;

        public void Show(System.Windows.Visibility visibility)
        {
            baseVisual.Visibility = visibility;
        }

        public void UpdateCharge(double charge, Tuple<double, double> range)
        {
            var scale = Math.Max(Math.Abs(range.Item1), Math.Abs(range.Item2));
            if (scale < 0.00001) scale = 0.00001;
            var t = Math.Abs(charge) / scale;
            this.model.Radius = smallRadius + Math.Min(t, 1.0) * (bigRadius - smallRadius);
            this.material = Materials.GetAtomBrush(charge, range);
            UpdateMaterial();
        }

        public void RemoveCharges()
        {
            this.model.Radius = 0.25;
            this.material = Materials.DefaultAtomBrush;
            UpdateMaterial();
        }

        public void UpdateMaterial()
        {
            baseVisual.Fill = model.Atom.IsHighlighted ? Materials.HighlightBrush : material;
        }

        void Atom_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (disposed) return;

            if (e.PropertyName.Equals("IsHighlighted", StringComparison.Ordinal))
            {
                UpdateMaterial();
                if (model.Atom.IsHighlighted) chargeVisual.ShowToolip(model);
                else chargeVisual.HideTooltip();
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
            model.Dispose();
            model = null;
        }
    }
}
