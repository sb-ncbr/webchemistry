using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using WebChemistry.Queries.Silverlight.ViewModel;
using WebChemistry.Queries.Silverlight.DataModel;


namespace WebChemistry.Queries.Silverlight.Visuals
{
    public class MotiveVisual3DWrap : Visual3D
    {
        FrameworkElement tooltipVisual;

        public bool SuppressTooltip { get; set; }

        public class TooltipInfo
        {
            public string Name { get; set; }
            public int SerialNumber { get; set; }
            public string Residue { get; set; }
            public int ResidueSequenceNumber { get; set; }
            public string ChainIdentifier { get; set; }
        }

        public IVisual Visual { get; private set; }

        internal void ShowToolip(AtomModel3D atom)
        {
            if (SuppressTooltip) return;

            tooltipVisual.DataContext =
                new TooltipInfo
                { 
                    Name = atom.Atom.PdbName(), 
                    SerialNumber = atom.Atom.PdbSerialNumber(), 
                    Residue = atom.Atom.PdbResidueName(), 
                    ResidueSequenceNumber = atom.Atom.PdbResidueSequenceNumber(),
                    ChainIdentifier = atom.Atom.PdbChainIdentifier().ToString()
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
        
        void SetCameraRadius()
        {
            var radius = Visual.CenterInfo.Radius;
            if (radius < 10) radius *= 4;
            else if (radius < 50) radius *= 3.2;
            else if (radius < 100) radius *= 2.7;
            else radius *= 2.5;

            if (radius < 1) radius = 1;
            else if (radius > 155) radius = 155;
                        
            Viewport.Camera.Radius = radius;
        }
                
        public void SetVisual(IVisual visual)
        {
            Clear();
            if (visual == null) return;

            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            this.Visual = visual;
            visual.Visual.Attach(this);
            visual.Visual.Register(_viewport);

            SetCameraRadius();

            Viewport.SuspendRender = false;
            Viewport.Render();
        }

        public void Clear()
        {
            Viewport.CancelRender();
            Viewport.SuspendRender = true;

            Visual = null;
            Viewport.Canvas.Children.Clear();

            if (Visual != null)
            {
                Visual.ClearVisual();
            }

            Viewport.Canvas.Children.Add(tooltipVisual);
            
            Viewport.SuspendRender = false;
            Viewport.Render();
        }

        public override void Register(Viewport3DBase viewport)
        {
            base.Register(viewport);
            viewport.Canvas.Children.Add(tooltipVisual);

            if (Visual != null) Visual.Visual.Register(viewport);
        }

        public override void Render(RenderContext context)
        {
            if (Visual != null) Visual.Visual.Render(context);
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            if (Visual != null) Visual.Visual.UpdateAsync(args);
        }

        public void Render()
        {
            Viewport.CancelRender();
            Viewport.Render();
        }

        public override void Dispose()
        {
            Clear();
        }

        public MotiveVisual3DWrap()
        {
            SuppressTooltip = true;

            tooltipVisual = (Application.Current.Resources["VisualTooltipTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
            tooltipVisual.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
