using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.Charges.Silverlight.Visuals
{
    public class BondVisual : VisualElement3D
    {
        Line wireVisual;
        BondModel3D model;
        Brush upMaterial, downMaterial;
                
        Point pA, pB;
        double width;
        int z;

        public IAtom AtomA { get { return model.A.Atom; } }
        public IAtom AtomB { get { return model.B.Atom; } }

        public void Show(System.Windows.Visibility visibility)
        {
            wireVisual.Visibility = visibility;
        }
        
        public void UpdateCharge(double chargeA, double chargeB, Tuple<double, double> range)
        {
            this.upMaterial = Materials.GetBondBrush(chargeA, chargeB, range);
            this.downMaterial = Materials.GetBondBrush(chargeB, chargeA, range);
            wireVisual.Stroke = pB.X > pA.X ? downMaterial : upMaterial;
        }

        public void RemoveCharges()
        {
            this.upMaterial = this.downMaterial = Materials.DefaultBondBrush;
            wireVisual.Stroke = upMaterial;
        }

        private void UpdateVisual()
        {
            if (pB.X > pA.X)
            {
                wireVisual.Stroke = upMaterial;
            }
            else
            {
                wireVisual.Stroke = downMaterial;
            }

            wireVisual.SetValue(Canvas.ZIndexProperty, z);
            wireVisual.X1 = pA.X;
            wireVisual.Y1 = pA.Y;
            wireVisual.X2 = pB.X;
            wireVisual.Y2 = pB.Y;
            wireVisual.StrokeThickness = width;
        }
    
        public BondVisual(BondModel3D model)
        {
            this.model = model;

            this.model.Radius = 0.25;
            this.wireVisual = VisualsManager<Line>.Withdraw();
            this.wireVisual.StrokeDashArray = null;
            this.wireVisual.IsHitTestVisible = false;
            this.wireVisual.VerticalAlignment = VerticalAlignment.Stretch;
            this.wireVisual.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.wireVisual.Visibility = Visibility.Visible;
        }
                
        public override void Render(RenderContext context)
        {
            var a = model.A;
            var b = model.B;
            var aT = a.TransformedCenter;
            var bT = b.TransformedCenter;

            double tdX = (bT.X - aT.X);
            double tdY = (bT.Y - aT.Y);

            if (tdX * tdX + tdY * tdY < 0.1)
            {
                return;
            }

            var atomDistance = model.Length;

            var distanceRatio = 0.9 / atomDistance;

            //var aR = a.Radius * distanceRatio;
            //var bR = b.Radius * distanceRatio;

            //var pA = new Point(aT.X + tdX * aR, aT.Y + tdY * aR);
            //var pB = new Point(bT.X - tdX * bR, bT.Y - tdY * bR);

            var pA = aT;
            var pB = bT;


            double dX = pB.X - pA.X;
            double dY = pB.Y - pA.Y;

            double width = 0.4 * model.Radius * (a.BoundingBox.Width / a.Radius + b.BoundingBox.Width / b.Radius);

            int z;

            int zA = a.ZIndex;
            int zB = b.ZIndex;
            int zpA = zA;
            int zpB = zB;

            int bottom = System.Math.Max(zpA, zpB);
            int top = System.Math.Min(zA, zB);
            z = (bottom + top) / 2;

            if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;

            this.pA = pA;
            this.pB = pB;
            this.width = width;
            this.z = z;
            UpdateVisual();
        }

        public override void Register(Viewport3DBase viewport)
        {
            viewport.Canvas.Children.Add(wireVisual);
        }
        
        public override void Dispose()
        {
            VisualsManager<Line>.Deposit(wireVisual);

            this.wireVisual = null;
            this.model.Dispose();
            this.model = null;
        }
    }
}
