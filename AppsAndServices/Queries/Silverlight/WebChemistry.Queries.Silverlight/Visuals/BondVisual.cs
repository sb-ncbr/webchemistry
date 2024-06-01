using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.Queries.Silverlight.Visuals
{
    static class BackgroundMaterial
    {
        public static Brush Brush { get; set; }

        static BackgroundMaterial()
        {
            Brush = new SolidColorBrush(Color.FromArgb(0xFF, 195, 195, 195));
        }
    }

    public class BondVisual : VisualElement3D
    {
        Line wireVisual;
        BondModel3D model;
        StructureMaterial material;
        
        Point pA, pB;
        double length, angle, width;
        int z;

        bool isBackground;

        void Update()
        {
            if (model.Bond.Type == Framework.Core.BondType.Metallic)
            {
                if (wireVisual == null) wireVisual = VisualsManager<Line>.Withdraw();

                this.wireVisual.StrokeDashArray = new DoubleCollection { 3, 5 };
                this.wireVisual.IsHitTestVisible = false;
                this.wireVisual.VerticalAlignment = VerticalAlignment.Stretch;
                this.wireVisual.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.wireVisual.Visibility = Visibility.Visible;
            }
            else
            {
                if (wireVisual == null) wireVisual = VisualsManager<Line>.Withdraw();

                this.wireVisual.StrokeDashArray = null;
                this.wireVisual.IsHitTestVisible = false;
                this.wireVisual.VerticalAlignment = VerticalAlignment.Stretch;
                this.wireVisual.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.wireVisual.Visibility = Visibility.Visible;
            }

            UpdateMaterial();
        }

        void UpdateMaterial()
        {
            if (model.A.Atom.IsSelected && model.B.Atom.IsSelected)
            {
                this.isBackground = false;
                this.wireVisual.Stroke = material.Brush;
            }
            else
            {
                this.isBackground = true;
                this.wireVisual.Stroke = BackgroundMaterial.Brush;
            }
        }
        
        private void UpdateVisual()
        {
            wireVisual.SetValue(Canvas.ZIndexProperty, z);
            wireVisual.X1 = pA.X;
            wireVisual.Y1 = pA.Y;
            wireVisual.X2 = pB.X;
            wireVisual.Y2 = pB.Y;
            wireVisual.StrokeThickness = width / (isBackground ? 5.0 : 2.5);            
        }

        public BondVisual(BondModel3D model, StructureMaterial material)
        {
            this.material = material;
            this.model = model;

            Update();
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

            var aR = a.Radius * distanceRatio;
            var bR = b.Radius * distanceRatio;

            var pA = new Point(aT.X + tdX * aR, aT.Y + tdY * aR);
            var pB = new Point(bT.X - tdX * bR, bT.Y - tdY * bR);
            
            double dX = pB.X - pA.X;
            double dY = pB.Y - pA.Y;

            double length = System.Math.Sqrt(dX * dX + dY * dY);
            double angle = MathHelper.RadiansToDegrees(System.Math.Atan2(dY, dX));
            double width = 0.4 * model.Radius * (a.BoundingBox.Width / a.Radius + b.BoundingBox.Width / b.Radius);

            int z;

            int zA = a.ZIndex;
            int zB = b.ZIndex;
            int zpA = zA;
            int zpB = zB;

          //  if (dA.dZ > 0)
            {
                int bottom = System.Math.Max(zpA, zpB);
                int top = System.Math.Min(zA, zB);
                z = (bottom + top) / 2;
            }
            //else
            //{
            //    int bottom = System.Math.Min(zA, zB);
            //    int top = System.Math.Min(zpA, zpB);
            //    z = (bottom + top) / 2;
            //}

            if (z > Int16.MaxValue - 1000) z = Int16.MaxValue - 1000;
            //visual.Update(origin, width, length, angle, z);           
            
            this.pA = pA;
            this.pB = pB;
            this.length = length;
            this.angle = angle;
            this.width = width;
            this.z = z;
            UpdateVisual();
        }        

        public override void Register(Viewport3DBase viewport)
        {
            if (wireVisual != null) viewport.Canvas.Children.Add(wireVisual);
        }

        public override void Dispose()
        {
            VisualsManager<Line>.Deposit(wireVisual);

            this.wireVisual = null;
            this.model = null;
        }
    }
}
