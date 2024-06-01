using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Math;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.SiteBinder.Silverlight.Visuals
{
    public class BondVisual : VisualElement3D
    {
        Line wireVisual;
        BondPath3D thickVisual, highlightAVisual, highlightBVisual;
        BondModel3D model;
        StructureMaterial material;
        bool upBrush;

        Point pA, pB;
        double length, angle, width;
        int z;

        RenderMode renderMode;

        bool visible = true;
        AtomDisplayMode atomDisplayMode;
        public AtomDisplayMode AtomDisplayMode
        {
            set { atomDisplayMode = value; UpdateVisibility(); }
        }

        public RenderMode RenderMode
        {
            set
            {
                renderMode = value;
                UpdateRenderMode();
            }
        }

        void UpdateRenderMode()
        {
            if (renderMode == SiteBinder.RenderMode.Nothing)
            {
                VisualsManager<BondPath3D>.Deposit(thickVisual);
                VisualsManager<BondPath3D>.Deposit(highlightAVisual);
                VisualsManager<BondPath3D>.Deposit(highlightBVisual);
                VisualsManager<Line>.Deposit(wireVisual);

                this.thickVisual = null;
                this.highlightAVisual = null;
                this.highlightBVisual = null;
                this.wireVisual = null;

                return;
            }

            if (model.Bond.Type == Framework.Core.BondType.Metallic)
            {
                if (wireVisual == null) wireVisual = VisualsManager<Line>.Withdraw();

                this.wireVisual.StrokeDashArray = new DoubleCollection { 3, 5 };
                this.wireVisual.IsHitTestVisible = false;
                this.wireVisual.VerticalAlignment = VerticalAlignment.Stretch;
                this.wireVisual.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.wireVisual.Stroke = material.Brush;

                return;
            }

            if (renderMode == SiteBinder.RenderMode.BallsAndSticks)
            {
                VisualsManager<BondPath3D>.Deposit(highlightAVisual);
                VisualsManager<BondPath3D>.Deposit(highlightBVisual);
                VisualsManager<Line>.Deposit(wireVisual);
                this.highlightAVisual = null;
                this.highlightBVisual = null;
                this.wireVisual = null;

                if (thickVisual == null) thickVisual = VisualsManager<BondPath3D>.Withdraw();
                
                this.thickVisual.StrokeThickness = 0;
                this.thickVisual.IsHitTestVisible = false;
                this.thickVisual.Visibility = Visibility.Visible;
            }
            else if (renderMode == SiteBinder.RenderMode.Sticks)
            {
                VisualsManager<Line>.Deposit(wireVisual);
                this.wireVisual = null;

                if (thickVisual == null) thickVisual = VisualsManager<BondPath3D>.Withdraw();
                if (highlightAVisual == null) highlightAVisual = VisualsManager<BondPath3D>.Withdraw();
                if (highlightBVisual == null) highlightBVisual = VisualsManager<BondPath3D>.Withdraw();
                
                this.thickVisual.StrokeThickness = 0;
                this.thickVisual.IsHitTestVisible = false;
                this.thickVisual.Visibility = Visibility.Visible;

                this.highlightAVisual.StrokeThickness = 0;
                this.highlightAVisual.IsHitTestVisible = false;
                this.highlightAVisual.Visibility = Visibility.Collapsed;

                this.highlightBVisual.StrokeThickness = 0;
                this.highlightBVisual.IsHitTestVisible = false;
                this.highlightBVisual.Visibility = Visibility.Collapsed;

                UpdateBondPart(highlightAVisual, model.A.Atom.IsSelected, model.A.Atom.IsHighlighted, false);
                UpdateBondPart(highlightBVisual, model.B.Atom.IsSelected, model.B.Atom.IsHighlighted, true);
            }
            else // wireframe
            {
                VisualsManager<BondPath3D>.Deposit(thickVisual);
                VisualsManager<BondPath3D>.Deposit(highlightAVisual);
                VisualsManager<BondPath3D>.Deposit(highlightBVisual);

                this.thickVisual = null;
                this.highlightAVisual = null;
                this.highlightBVisual = null;

                if (wireVisual == null) wireVisual = VisualsManager<Line>.Withdraw();

                this.wireVisual.StrokeDashArray = null;
                this.wireVisual.IsHitTestVisible = false;
                this.wireVisual.VerticalAlignment = VerticalAlignment.Stretch;
                this.wireVisual.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.wireVisual.Stroke = material.Brush;
            }
        }
        
        void Collapse(UIElement e)
        {
            if (e.Visibility == Visibility.Visible) e.Visibility = Visibility.Collapsed;
        }

        void Show(UIElement e)
        {
            if (e.Visibility == Visibility.Collapsed) e.Visibility = Visibility.Visible;
        }

        private void UpdateVisual()
        {
            if (!visible)
            {
                if (highlightAVisual != null) Collapse(highlightAVisual);
                if (highlightBVisual != null) Collapse(highlightBVisual);
                if (wireVisual != null) Collapse(wireVisual);
                if (thickVisual != null) Collapse(thickVisual);
                return;
            }

            if (wireVisual != null)
            {
                Show(wireVisual);
                wireVisual.SetValue(Canvas.ZIndexProperty, z);
                wireVisual.X1 = pA.X;
                wireVisual.Y1 = pA.Y;
                wireVisual.X2 = pB.X;
                wireVisual.Y2 = pB.Y;
                wireVisual.StrokeThickness = width / 4;

                return;
            }

            if (renderMode == RenderMode.Sticks)
            {
                bool processVisual = true;

                if ((model.A.Atom.IsSelected || model.A.Atom.IsHighlighted) &&
                    (model.B.Atom.IsSelected || model.B.Atom.IsHighlighted))
                {
                    UpdateBondPart(highlightAVisual, model.A.Atom.IsSelected, model.A.Atom.IsHighlighted, false);
                    UpdateBondPart(highlightBVisual, model.B.Atom.IsSelected, model.B.Atom.IsHighlighted, true);
                    Collapse(thickVisual);
                    processVisual = false;
                }
                else if (model.A.Atom.IsSelected || model.A.Atom.IsHighlighted)
                {
                    UpdateBondPart(highlightAVisual, model.A.Atom.IsSelected, model.A.Atom.IsHighlighted, false);
                }
                else if (model.B.Atom.IsSelected || model.B.Atom.IsHighlighted)
                {
                    UpdateBondPart(highlightBVisual, model.B.Atom.IsSelected, model.B.Atom.IsHighlighted, true);
                }

                if (!model.A.Atom.IsHighlighted && !model.A.Atom.IsSelected) Collapse(highlightAVisual);
                if (!model.B.Atom.IsHighlighted && !model.B.Atom.IsSelected) Collapse(highlightBVisual);

                
                if (processVisual)
                {
                    Show(thickVisual);
                    thickVisual.Update(width, length, angle);
                    thickVisual.SetValue(Canvas.LeftProperty, (pA.X) - width / 4);
                    thickVisual.SetValue(Canvas.TopProperty, (pA.Y) - width / 2);
                    thickVisual.SetValue(Canvas.ZIndexProperty, z);
                    thickVisual.Fill = upBrush ? material.UpBrush : material.BottomBrush;
                }
            }
            else if (renderMode == SiteBinder.RenderMode.BallsAndSticks)
            {
                Show(thickVisual);
                thickVisual.Update(width, length, angle);
                thickVisual.SetValue(Canvas.LeftProperty, (pA.X) - width / 4);
                thickVisual.SetValue(Canvas.TopProperty, (pA.Y) - width / 2);
                thickVisual.SetValue(Canvas.ZIndexProperty, z);
                thickVisual.Fill = upBrush ? material.UpBrush : material.BottomBrush;
            }
        }

        void UpdateBondPart(BondPath3D part, bool selected, bool highlighted, bool isB)
        {
            if (model.Bond.Type == BondType.Metallic) return;

            if (selected || highlighted)
            {
                if (!isB)
                {
                    highlightAVisual.Update(width, length / 2, angle);
                    highlightAVisual.SetValue(Canvas.LeftProperty, (pA.X) - width / 4);
                    highlightAVisual.SetValue(Canvas.TopProperty, (pA.Y) - width / 2);
                    highlightAVisual.SetValue(Canvas.ZIndexProperty, z + 1);
                }
                else
                {
                    highlightBVisual.Update(width, length / 2, 180 + angle);
                    highlightBVisual.SetValue(Canvas.LeftProperty, (pB.X) - width / 4);
                    highlightBVisual.SetValue(Canvas.TopProperty, (pB.Y) - width / 2);
                    highlightBVisual.SetValue(Canvas.ZIndexProperty, z + 1);
                }
            }

            if (renderMode == RenderMode.BallsAndSticks)
            {
                part.Fill = null;
                Collapse(part);
            }
            else
            {
                if (highlighted)
                {
                    Show(part);
                    part.Fill = AtomMaterials.HighlightBondBrush;
                }
                else
                {
                    if (selected)
                    {
                        part.Visibility = Visibility.Visible;
                        if (isB)
                        {
                            part.Fill = !upBrush ? AtomMaterials.SelectBondMaterial.UpBrush : AtomMaterials.SelectBondMaterial.BottomBrush;
                        }
                        else
                        {
                            part.Fill = upBrush ? AtomMaterials.SelectBondMaterial.UpBrush : AtomMaterials.SelectBondMaterial.BottomBrush;
                        }
                    }
                    else
                    {
                        part.Fill = null;
                        Collapse(part);
                    }
                }
            }
        }

        public void UpdateVisibility()
        {
            if (atomDisplayMode == SiteBinder.AtomDisplayMode.All) visible = true;
            else visible = model.A.Atom.IsSelected && model.B.Atom.IsSelected;

            UpdateVisual();
        }

        public void Highlight(IAtom a)
        {
            if (renderMode == SiteBinder.RenderMode.Sticks)
            {
                UpdateVisual();
            }
        }

        public void Select(IAtom a)
        {
            if (renderMode == SiteBinder.RenderMode.Sticks)
            {
                UpdateVisual();
            }
        }

        public BondVisual(BondModel3D model, StructureMaterial material)
        {
            this.material = material;
            this.model = model;

            model.A.Atom.PropertyChanged += Atom_PropertyChanged;
            model.B.Atom.PropertyChanged += Atom_PropertyChanged;
        }

        void Atom_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (renderMode == SiteBinder.RenderMode.Nothing || disposed) return;

            if (e.PropertyName.Equals("IsSelected", StringComparison.Ordinal))
            {
                Select(sender as IAtom);
                UpdateVisibility();
            }
            else if (e.PropertyName.Equals("IsHighlighted", StringComparison.Ordinal))
            {
                Highlight(sender as IAtom);
            }
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
            
            bool upBrush = true;
            double sa = angle - 45;

            //if (sa >= 0 && sa <= 90 ) quadrant = 1;
            //if ((sa > 90 && sa <= 180) || (sa < -90 && sa > -180)) upBrush = false;
            //else if (sa < -90 && sa > -180) quadrant = 4;
            //else if (sa < 0 && sa >= -90) quadrant = 3;

            if (sa < -90 || sa > 90) upBrush = false;

            this.upBrush = upBrush;
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
            if (thickVisual != null) viewport.Canvas.Children.Add(thickVisual);
            if (highlightAVisual != null) viewport.Canvas.Children.Add(highlightAVisual);
            if (highlightBVisual != null) viewport.Canvas.Children.Add(highlightBVisual);
            if (wireVisual != null) viewport.Canvas.Children.Add(wireVisual);
        }

        bool disposed = false;

        public override void Dispose()
        {
            disposed = true;
            VisualsManager<BondPath3D>.Deposit(thickVisual);
            VisualsManager<BondPath3D>.Deposit(highlightAVisual);
            VisualsManager<BondPath3D>.Deposit(highlightBVisual);
            VisualsManager<Line>.Deposit(wireVisual);

            this.thickVisual = null;
            this.highlightAVisual = null;
            this.highlightBVisual = null;
            this.wireVisual = null;

            if (model != null)
            {
                model.A.Atom.PropertyChanged -= Atom_PropertyChanged;
                model.B.Atom.PropertyChanged -= Atom_PropertyChanged;
                model = null;
            }
        }
    }
}
