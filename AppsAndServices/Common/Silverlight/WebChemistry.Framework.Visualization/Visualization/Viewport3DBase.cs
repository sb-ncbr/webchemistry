using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WebChemistry.Framework.Math;

namespace WebChemistry.Framework.Visualization
{
    public class Viewport3DBase : UserControl, IDisposable
    {
        Canvas _canvas;
        BackgroundWorker[] _renderers;

        int RendererCount { get; set; }

        public bool SuspendRender { get; set; }

        public Canvas Canvas { get { return _canvas; } }

        double _width = 1;
        double _height = 1;        
        Matrix3D _screenToViewTransform;
        Matrix3D _ndcToScreenTransform;
        
        OrthogonalCamera _camera;
        public OrthogonalCamera Camera
        {
            get { return _camera; }
        }

        Visual3DCollection _children = new Visual3DCollection();

        object _syncRoot = new object();

        #region Properties
        
        public bool IsRendering
        {
            get { return (bool)GetValue(IsRenderingProperty); }
            protected set
            {
                try
                {
                    _changingIsRendering = true;
                    SetValue(IsRenderingProperty, value);
                }
                finally
                {
                    _changingIsRendering = false;
                }
            }
        }

        private bool _changingIsRendering;

        public static readonly DependencyProperty IsRenderingProperty = DependencyProperty.Register(
            "IsRendering", typeof(bool), typeof(Viewport3DBase), new PropertyMetadata(false, OnIsRenderingChanged));

        private static void OnIsRenderingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DBase viewport = (Viewport3DBase)d;
            if (!viewport._changingIsRendering)
            {
                viewport.IsRendering = (bool)e.OldValue;
                throw new InvalidOperationException("'IsRendering' property is read-only and cannot be modified.");
            }
        }
        
        #endregion
                       
        public Viewport3DBase()
        {
            SuspendRender = false;

            _screenToViewTransform = new Matrix3D(0, 0, 0, 0,
                                                  0, 0, 0, 0,
                                                 -1, 0, 0, 0,
                                                  0, 0, 0, 1);
            _ndcToScreenTransform = new Matrix3D(0, 0, 0, 0,
                                                 0, 0, 0, 0,
                                                 0, 0, 0, 0,
                                                 0, 0, 0, 1);
                   
            _canvas = new Canvas();
            _canvas.CacheMode = new BitmapCache();
            this.Content = _canvas;

            RendererCount = System.Environment.ProcessorCount;

            _renderers = new BackgroundWorker[RendererCount];

            for (int i = 0; i < RendererCount; i++)
            {
                _renderers[i] = new BackgroundWorker();
                _renderers[i].WorkerSupportsCancellation = true;
                _renderers[i].WorkerReportsProgress = false;
                _renderers[i].DoWork += new DoWorkEventHandler(RenderStart);
                _renderers[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(RenderCompleted);   
            }

            _camera = new OrthogonalCamera(this) { Yaw = 0, Pitch = 0, Radius = 25 };

            SizeChanged += new SizeChangedEventHandler(Viewport_SizeChanged);

          //  int wt, cpt;
         //   System.Threading.ThreadPool.GetMaxThreads(out wt, out cpt);
            //System.Diagnostics.Debug.WriteLine("{0} {1}", wt, cpt);
        }

        void Viewport3D_LayoutUpdated(object sender, EventArgs e)
        {
            _width = ActualWidth;
            _height = ActualHeight;

            _camera.AspectRatio = _width / _height;

            UpdateScreenToViewTransformMatrix();
            UpdateNdcToScreenTransformMatrix();

            Render();
        }

        void LockCanvas()
        {
            _canvas.Visibility = System.Windows.Visibility.Collapsed;
        }

        void UnlockCanvas()
        {
            _canvas.Visibility = System.Windows.Visibility.Visible;
        }

        public void SetVisuals(IEnumerable<Visual3D> visuals)
        {
            LockCanvas();

            Clear();

            foreach (var v in visuals)
            {
                //v.Viewport = this;
                v.Register(this);
                _children.Add(v);
            }
                       
            UnlockCanvas();
        }

        //public void AddVisuals(IEnumerable<Visual3D> visuals)
        //{
        //    LockCanvas();

        //    foreach (var v in visuals)
        //    {
        //        v.RegisterVisuals(this);
        //        Children.Add(v);
        //    }

        //    UnlockCanvas();
        //}

        //public void AddVisual(Visual3D visual)
        //{
        //    LockCanvas();

        //    visual.RegisterVisuals(this);
        //    Children.Add(visual);

        //    UnlockCanvas();
        //}

        //public void RemoveVisuals(IEnumerable<Visual3D> visuals)
        //{
        //    throw new NotImplementedException();
        //}

        //public void RemoveVisual(Visual3D visual)
        //{
        //    LockCanvas();

        //    visual.UnregisterVisuals(this);
        //    Children.Remove(visual);

        //    UnlockCanvas();
        //}
        
        void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _width = e.NewSize.Width;
            _height = e.NewSize.Height;

            _camera.AspectRatio = _width / _height;

            UpdateScreenToViewTransformMatrix();
            UpdateNdcToScreenTransformMatrix();

            Render();
        }

        bool _renderAfterCancelled = true;
        bool _cancelled = false;
        bool _rendered = false;

        public void CancelRender(bool renderAfterCancelled = false)
        {
            _renderAfterCancelled = renderAfterCancelled;

            foreach (var renderer in _renderers)
            {
                if (renderer.IsBusy)
                {
                    renderer.CancelAsync();
                    _cancelled = true;
                }
            }
        }

        public void Render()
        {
            if (SuspendRender
                || _children.Count == 0
                || Visibility == System.Windows.Visibility.Collapsed 
                || Opacity == 0.0 
                || _width == 0 
                || _height == 0
                || DesignerProperties.IsInDesignTool) return;
            
            lock (_syncRoot)
            {
                CancelRender(true);

                if (_cancelled)
                {
                    if (!_rendered) return;
                    else _cancelled = false;
                }
            }

            _rendered = false;
            IsRendering = true;

            for (int i = 0; i < RendererCount; i++)
            {
                _renderers[i].RunWorkerAsync(i); 
            }
        }


        public void RenderSynchronously()
        {
            _rendered = false;
            IsRendering = true;

            UpdateContext context = new UpdateContext()
            {
                CameraPosition = _camera.Position,
                CameraUpVector = _camera.UpDirection,
                WorldTransform = _camera.WorldTransform,
                WorldToNdc = _camera.Value,
                NdcToScreen = _ndcToScreenTransform
            };
            
            for (int i = 0; i < RendererCount; i++)
            {
                UpdateAsyncArgs args = new UpdateAsyncArgs()
                {
                    Worker = null,
                    WorkerArgs = new DoWorkEventArgs(i),
                    Context = context,
                    WorkerIndex = i,
                    WorkerCount = RendererCount
                };
                foreach (var visual in _children) visual.UpdateAsync(args);
            }

            _canvas.Visibility = System.Windows.Visibility.Collapsed;

            RenderContext rcontext = new RenderContext();

            foreach (var visual in _children)
            {
                try
                {
                    visual.Render(rcontext);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Rendering error:\n" + ex.Message);
                }
            }

            _canvas.Visibility = System.Windows.Visibility.Visible;
            IsRendering = false;

            _rendered = true;

            _rendered = true;
            IsRendering = false;
        }
                
        void RenderStart(object sender, DoWorkEventArgs e)
        {   
            UpdateContext context = new UpdateContext()
            {
                CameraPosition = _camera.Position,
                CameraUpVector = _camera.UpDirection,
                WorldTransform = _camera.WorldTransform,
                WorldToNdc = _camera.Value,
                NdcToScreen = _ndcToScreenTransform
            };
            
            UpdateAsyncArgs args = new UpdateAsyncArgs()
            {
                Worker = sender as BackgroundWorker,
                WorkerArgs = e,
                Context = context,
                WorkerIndex = (int)e.Argument,
                WorkerCount = RendererCount
            };
                        
            foreach (var visual in _children)
            {              
                visual.UpdateAsync(args);
            }
        }        
        
        void RenderCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_renderers.All(r => !r.IsBusy) && !_rendered)
            {
                if (_cancelled)
                {
                    lock (_syncRoot)
                    {
                        _cancelled = false;
                        IsRendering = false;
                        _rendered = true;
                    }
                    if (_renderAfterCancelled) Render();
                }
                else
                {
                    _canvas.Visibility = System.Windows.Visibility.Collapsed;

                    RenderContext context = new RenderContext();

                    foreach (var visual in _children)
                    {
                        try
                        {
                            visual.Render(context);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Rendering error:\n" + ex.Message);
                        }
                    }

                    _canvas.Visibility = System.Windows.Visibility.Visible;
                    IsRendering = false;

                    _rendered = true;
                }
            }
        }
        
        public void Clear()
        {
            Canvas.Children.Clear();

            foreach (var c in _children)
            {
                c.Dispose();
            }

            _children.Clear();
        }

        private void UpdateNdcToScreenTransformMatrix()
        {
            double screenDepth = 1;

            _ndcToScreenTransform.M11 = _width / 2;
            _ndcToScreenTransform.M22 = -_height / 2;
            _ndcToScreenTransform.M33 = screenDepth / 2;
            _ndcToScreenTransform.OffsetX = _ndcToScreenTransform.M11;
            _ndcToScreenTransform.OffsetY = -_ndcToScreenTransform.M22;
            _ndcToScreenTransform.OffsetZ = _ndcToScreenTransform.M33;
        }

        private void UpdateScreenToViewTransformMatrix()
        {
            double depth = 1.0 / System.Math.Tan(System.Math.PI * Camera.FieldOfView / 360.0);

            _screenToViewTransform.M11 = 2 / _width;
            _screenToViewTransform.M22 = -2 / (_height * _camera.AspectRatio);
            _screenToViewTransform.M32 = 1 / _camera.AspectRatio;
            _screenToViewTransform.M33 = -depth;
        }

        public void Dispose()
        {
            CancelRender();
            Clear();

            for (int i = 0; i < RendererCount; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].DoWork -= new DoWorkEventHandler(RenderStart);
                    _renderers[i].RunWorkerCompleted -= new RunWorkerCompletedEventHandler(RenderCompleted);
                    _renderers[i] = null;
                }
            }
        }
    }
}
