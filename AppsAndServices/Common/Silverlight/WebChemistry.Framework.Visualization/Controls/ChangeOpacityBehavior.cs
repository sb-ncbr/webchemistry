using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System;
using System.Linq;
using System.Windows.Media;

namespace WebChemistry.Framework.Controls
{
    public class ChangeOpacityBehavior : Behavior<FrameworkElement>
    {
        Storyboard _goNormal;
        Storyboard _goMouseOver;

        public double NormalOpacity
        {
            get { return (double)GetValue(NormalOpacityProperty); }
            set { SetValue(NormalOpacityProperty, value); }
        }

        public static readonly DependencyProperty NormalOpacityProperty =
            DependencyProperty.Register("NormalOpacity", typeof(double), typeof(ChangeOpacityBehavior), new PropertyMetadata(0.5));



        public double MouseOverOpacity
        {
            get { return (double)GetValue(MouseOverOpacityProperty); }
            set { SetValue(MouseOverOpacityProperty, value); }
        }

        public static readonly DependencyProperty MouseOverOpacityProperty =
            DependencyProperty.Register("MouseOverOpacity", typeof(double), typeof(ChangeOpacityBehavior), new PropertyMetadata(1.0));

        bool _isMouseDown = false;

        protected override void OnAttached()
        {
            base.OnAttached();

            _goNormal = new Storyboard();
            DoubleAnimation normalOpacity = new DoubleAnimation() { From = MouseOverOpacity, To = NormalOpacity, Duration = new Duration(TimeSpan.Parse("0:0:0.3")) };
            Storyboard.SetTarget(normalOpacity, AssociatedObject);
            Storyboard.SetTargetProperty(normalOpacity, new PropertyPath("Opacity"));
            _goNormal.Children.Add(normalOpacity);

            _goMouseOver = new Storyboard();
            DoubleAnimation mouseOverOpacity = new DoubleAnimation() { From = NormalOpacity, To = MouseOverOpacity, Duration = new Duration(TimeSpan.Parse("0:0:0.3")) };
            Storyboard.SetTarget(mouseOverOpacity, AssociatedObject);
            Storyboard.SetTargetProperty(mouseOverOpacity, new PropertyPath("Opacity"));
            _goMouseOver.Children.Add(mouseOverOpacity);

            AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.MouseEnter += new System.Windows.Input.MouseEventHandler(AssociatedObject_MouseEnter);
            AssociatedObject.MouseLeave += new System.Windows.Input.MouseEventHandler(AssociatedObject_MouseLeave);

            //AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
           // AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;

            //AssociatedObject.AddHandler(FrameworkElement.MouseLeftButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler(AssociatedObject_MouseLeftButtonDown), true);            
            AssociatedObject.AddHandler(FrameworkElement.MouseLeftButtonUpEvent, new System.Windows.Input.MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp), true);
        }       
       
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.MouseEnter -= new System.Windows.Input.MouseEventHandler(AssociatedObject_MouseEnter);
            AssociatedObject.MouseLeave -= new System.Windows.Input.MouseEventHandler(AssociatedObject_MouseLeave);

            //AssociatedObject.RemoveHandler(FrameworkElement.MouseLeftButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler(AssociatedObject_MouseLeftButtonDown));
            AssociatedObject.RemoveHandler(FrameworkElement.MouseLeftButtonUpEvent, new System.Windows.Input.MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp));
        }

        //void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseEventArgs e)
        //{
        //    if (sender is UIElement) _isMouseDown = true;
        //}

        void AssociatedObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (AssociatedObject.Opacity != NormalOpacity) _goNormal.Begin();
        }

        void AssociatedObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isMouseDown) _goNormal.Begin();
        }

        void AssociatedObject_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _goMouseOver.Begin();
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).Opacity = NormalOpacity;
        }
    }
}