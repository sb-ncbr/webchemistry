using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Framework.Visualization;
using WebChemistry.Framework.Core;
using System.Windows.Media.Imaging;

namespace WebChemistry.Framework.Controls
{
    public partial class Viewport3D : UserControl
    {
        Point _dragOrigin, _mouseDownPoint;
        Point _zoomOrigin;
        bool _isMouseDown = false;
        bool _isMouseRightDown = false;

        Storyboard _animateYaw;
        bool _animatingYaw;

        public Visual3D Visual
        {
            get { return (Visual3D)GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        public static readonly DependencyProperty VisualProperty =
            DependencyProperty.Register("Visual", typeof(Visual3D), typeof(Viewport3D), new PropertyMetadata(null, OnVisualChanged));

        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        public static readonly DependencyProperty AnimateProperty =
            DependencyProperty.Register("Animate", typeof(bool), typeof(Viewport3D), new PropertyMetadata(false, OnAnimateChanged));

        private static void OnAnimateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Viewport3D;
            if ((bool)e.NewValue) control.BeginAnimation();
            else control.PauseAnimation();
        }

        private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Viewport3D).SetVisual(e.NewValue as Visual3D);
        }

        public bool CanSelectAtoms
        {
            get { return (bool)GetValue(CanSelectAtomsProperty); }
            set { SetValue(CanSelectAtomsProperty, value); }
        }

        public static readonly DependencyProperty CanSelectAtomsProperty =
            DependencyProperty.Register("CanSelectAtoms", typeof(bool), typeof(Viewport3D), new PropertyMetadata(false, OnCanSelectAtomsChanged));

        private static void OnCanSelectAtomsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Viewport3D;
            control.SetCursor(Cursors.Arrow);
        }

        //public bool SuspendRender
        //{
        //    get { return (bool)GetValue(SuspendRenderProperty); }
        //    set { SetValue(SuspendRenderProperty, value); }
        //}

        //public static readonly DependencyProperty SuspendRenderProperty =
        //    DependencyProperty.Register("SuspendRender", typeof(bool), typeof(Viewport3D), new PropertyMetadata(false, OnSuspendRenderChanged));

        //private static void OnSuspendRenderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var control = (d as Viewport3D);
        //    control.Viewport.SuspendRender = (bool)e.NewValue;
        //}

        public Viewport3D()
        {
            InitializeComponent();

            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                InitAnimations();
            }
        }

        private void InitAnimations()
        {
            _animateYaw = new Storyboard();

            DoubleAnimation da = new DoubleAnimation() { From = -180, To = 180, Duration = TimeSpan.Parse("0:0:5") };

            Storyboard.SetTarget(da, viewport.Camera);
            Storyboard.SetTargetProperty(da, new PropertyPath("Yaw"));

            da.RepeatBehavior = RepeatBehavior.Forever;
            _animateYaw.Children.Add(da);
        }        

        public Viewport3DBase Viewport
        {
            get { return viewport; }
        }

        private void overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            overlay.CaptureMouse();

            _isMouseDown = true;
            _dragOrigin = e.GetPosition(overlay);
            _mouseDownPoint = _dragOrigin;

            sliderZoom.IsHitTestVisible = false;
            sliderYaw.IsHitTestVisible = false;
            sliderPitch.IsHitTestVisible = false;
        }

        private void overlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            overlay.ReleaseMouseCapture();

            _isMouseDown = false;
            sliderZoom.IsHitTestVisible = true;
            sliderYaw.IsHitTestVisible = true;
            sliderPitch.IsHitTestVisible = true;

            if (_previouslyHighlighted != null)
            {
                if (CanSelectAtoms) _previouslyHighlighted.IsSelected = !_previouslyHighlighted.IsSelected;
            }
        }

        private void overlay_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            overlay.CaptureMouse();
            _isMouseRightDown = true;
            e.Handled = true;

            _zoomOrigin = e.GetPosition(overlay);
        }

        private void overlay_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            overlay.ReleaseMouseCapture();
            e.Handled = true;
            _isMouseRightDown = false;
        }

        void SetCursor(Cursor c)
        {
            if (overlay.Cursor != c)
            {
                overlay.Cursor = c;
            }
        }
        
        IAtom _previouslyHighlighted = null;
              
        private void overlay_MouseMove(object sender, MouseEventArgs e)
        {
            Point cursorPosition = e.GetSafePosition(overlay);
            
            if (_isMouseDown)
            {
                HandleMouseMoveRotation(cursorPosition);
            }

            if (_isMouseRightDown)
            {
                var dY = (_zoomOrigin.Y - cursorPosition.Y) / 3.0;
                _zoomOrigin = cursorPosition;
                sliderZoom.Value += dY;
            }

            if (_isMouseDown || _isMouseRightDown) return;

            if (_animatingYaw)
            {
                SetCursor(Cursors.Arrow);

                return;
            }

            var intersected = VisualTreeHelper.FindElementsInHostCoordinates(e.GetSafePosition(null), viewport.Canvas)
                .Where(el => el is Ellipse && (el as Ellipse).Tag is IAtom)
                .Select(el => (el as Ellipse).Tag as IAtom)
                .FirstOrDefault();

            if (intersected == null)
            {
                if (CanSelectAtoms) SetCursor(Cursors.Arrow);

                if (_previouslyHighlighted != null)
                {
                    _previouslyHighlighted.IsHighlighted = false;
                    _previouslyHighlighted = null;
                }
            }
            else
            {
                //for (int i = 1; i < intersected.Length; i++) intersected[i].IsHighlighted = false;

                if (_previouslyHighlighted != null && !object.ReferenceEquals(_previouslyHighlighted, intersected)) 
                {
                    _previouslyHighlighted.IsHighlighted = false;
                }

                if (CanSelectAtoms) SetCursor(Cursors.Hand);

                _previouslyHighlighted = intersected;
                _previouslyHighlighted.IsHighlighted = true;
            }
        }

        private void HandleMouseMoveRotation(Point cursorPosition)
        {
            SetCursor(Cursors.Arrow);

            if (_previouslyHighlighted != null)
            {
                double odX = _mouseDownPoint.X - cursorPosition.X;
                double odY = _mouseDownPoint.Y - cursorPosition.Y;

                if (odX * odX + odY * odY > 4)
                {
                    _previouslyHighlighted.IsHighlighted = false;
                    _previouslyHighlighted = null;
                }
            }

            var ov = overlay;
            double width = ov.ActualWidth / 3;
            double height = ov.ActualHeight / 3;

            double dX = _dragOrigin.X - cursorPosition.X;
            double dY = _dragOrigin.Y - cursorPosition.Y;

            double newYaw = sliderYaw.Value - 360 * dX / width;
            double newPitch = sliderPitch.Value - 360 * dY / height;

            if (newYaw < -180)
            {
                while (newYaw < -180) newYaw += 360;
            }
            else if (newYaw > 180)
            {
                while (newYaw > 180) newYaw -= 360;
            }

            if (newPitch < -180)
            {
                while (newPitch < -180) newPitch += 360;
            }
            else if (newPitch > 180)
            {
                while (newPitch > 180) newPitch -= 360;
            }

            if (_animatingYaw)
            {
                viewport.Camera.Pitch = newPitch;
            }
            else
            {
                viewport.Camera.SetYawPitch(newYaw, newPitch);
            }

            _dragOrigin = cursorPosition;
        }

        private void overlay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                sliderZoom.Value -= 1;
            }
            else
            {
                sliderZoom.Value += 1;
            }
        }

        void SetVisuals(IEnumerable<WebChemistry.Framework.Visualization.Visual3D> visuals)
        {
            if (System.ComponentModel.DesignerProperties.IsInDesignTool) return;
            
            viewport.SetVisuals(visuals);
        }

        public void Render()
        {
            viewport.Render();
        }

        private void BeginAnimation()
        {
            _animateYaw.Begin();
            _animatingYaw = true;
        }

        private void PauseAnimation()
        {
            _animateYaw.Pause();
            _animatingYaw = false;
        }

        private void UpdateClip(object sender, System.Windows.SizeChangedEventArgs e)
        {
        	ClipRect.Rect = new Rect(0, 0, ActualWidth, ActualHeight);
        }
                
        public void SetVisual(Visual3D visual)
        {
            if (System.ComponentModel.DesignerProperties.IsInDesignTool) return;

            if (visual != null)
            {
                var radius = visual.BoundingSphereRadius;

                if (radius < 1) radius = 10;
                if (radius < 10) radius *= 4;
                else if (radius < 50) radius *= 3.2;
                else if (radius < 100) radius *= 2.7;
                else radius *= 2.5;

                if (radius < 1) radius = 1;
                else if (radius > 155) radius = 155;

                visual.Viewport = this.Viewport;
                viewport.Camera.Radius = radius;
                viewport.SetVisuals(new Visual3D[] { visual });
                viewport.Render();
            }
            else
            {
                viewport.Clear();
            }
        }

        public void Clear()
        {
            viewport.Clear();
        }

        bool _wasAnimatingBeforeCollapse = false;

        private void LayoutRoot_LayoutUpdated(object sender, EventArgs e)
        {
            //if (Visibility == System.Windows.Visibility.Collapsed || Opacity == 0.0)
            //{
            //    if (_animatingYaw)
            //    {
            //        _animateYaw.Pause();
            //        _wasAnimatingBeforeCollapse = true;
            //    }
            //    else
            //    {
            //        _wasAnimatingBeforeCollapse = false;
            //    }
            //    Viewport.SuspendRender = true;
            //}
            //else
            //{
            //    if (_animatingYaw && _wasAnimatingBeforeCollapse)
            //    {
            //        _animateYaw.Resume();
            //    }
            //    _wasAnimatingBeforeCollapse = false;
            //    Viewport.SuspendRender = false;
            //}
        }

        public WriteableBitmap RenderToBitmap(int w, int h)
        {
            var height = viewportWrap.ActualHeight;
            var width = viewportWrap.ActualWidth;

            var scale = (double)h / height;
            var offset = ((width * scale) - w) / 2.0;
                

            WriteableBitmap bmp = new WriteableBitmap(w, h);

            //var t = new MatrixTransform { Matrix = Matrix.Identity };
            var st = new ScaleTransform { ScaleX = scale, ScaleY = scale };
            var ot = new TranslateTransform { X = -offset };
            var t = new TransformGroup();
            t.Children.Add(st);
            t.Children.Add(ot);
            bmp.Render(viewportWrap, t);

            return bmp;
        }
    }
}
