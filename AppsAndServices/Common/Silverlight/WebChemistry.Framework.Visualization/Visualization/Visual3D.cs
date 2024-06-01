using System;
using System.Windows;

namespace WebChemistry.Framework.Visualization
{
    public abstract class Visual3D : DependencyObject, IDisposable
    {
        #region Properties
        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register("Opacity", typeof(double), typeof(Visual3D), new PropertyMetadata(1.0, OnOpacityChangedInternal));

        static void OnOpacityChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Visual3D).OnOpacityChanged((double)e.NewValue);
        }

        protected virtual void OnOpacityChanged(double newValue)
        {

        }

        public Visibility Visibility
        {
            get { return (Visibility)GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        public static readonly DependencyProperty VisibilityProperty =
            DependencyProperty.Register("Visibility", typeof(Visibility), typeof(Visual3D), new PropertyMetadata(Visibility.Visible, OnVisibilityChangedInternal));

        static void OnVisibilityChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Visual3D).OnVisibilityChanged((Visibility)e.NewValue);
        }

        protected virtual void OnVisibilityChanged(Visibility newValue)
        {

        }
        
        #endregion

        //IEnumerable<Viewport3DBase> 

        protected Viewport3DBase _viewport;

        public Viewport3DBase Viewport
        {
            get { return _viewport; }
            set { _viewport = value; }
        }

        /// <summary>
        /// Bouding Sphere Radius
        /// </summary>
        public double BoundingSphereRadius { get; set; }

        /// <summary>
        /// Registers the sctructure to a particular viewport
        /// </summary>
        /// <param name="viewport"></param>
        public virtual void Register(Viewport3DBase viewport)
        {
            Viewport = viewport;
        }
        
        public abstract void Render(RenderContext context);
        public abstract void UpdateAsync(UpdateAsyncArgs args);
        
        public abstract void Dispose();
    }
}
