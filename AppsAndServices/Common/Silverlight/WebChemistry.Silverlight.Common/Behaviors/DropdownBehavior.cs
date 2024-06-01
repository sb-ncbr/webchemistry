using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace WebChemistry.Silverlight.Common
{
    public class ContextDropdownBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.MouseMove += new MouseEventHandler(AssociatedObject_MouseMove);
            this.AssociatedObject.MouseEnter += new MouseEventHandler(AssociatedObject_MouseMove);
            this.AssociatedObject.MouseRightButtonDown += new MouseButtonEventHandler(AssociatedObject_MouseRightButtonDown);

            if (this.AssociatedObject is Button)
            {
                (AssociatedObject as Button).Click += new RoutedEventHandler(AssociatedObject_Click);
            }
            else if (this.AssociatedObject is HyperlinkButton)
            {
                (AssociatedObject as HyperlinkButton).Click += new RoutedEventHandler(AssociatedObject_Click);
            }
        }

        void AssociatedObject_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        Point currentMousePositionWithinButton = new Point();
        void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            var button = sender as FrameworkElement;
            currentMousePositionWithinButton = e.GetSafePosition(button);
        }

        void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            var menu = ContextMenuService.GetContextMenu(this.AssociatedObject);
            
            if (menu == null) return;

            menu.HorizontalOffset = -currentMousePositionWithinButton.X;
            menu.VerticalOffset = this.AssociatedObject.ActualHeight - currentMousePositionWithinButton.Y;
            menu.Width = this.AssociatedObject.ActualWidth;
            menu.IsOpen = true;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.MouseMove -= new MouseEventHandler(AssociatedObject_MouseMove);
            this.AssociatedObject.MouseEnter -= new MouseEventHandler(AssociatedObject_MouseMove);
            this.AssociatedObject.MouseRightButtonDown -= new MouseButtonEventHandler(AssociatedObject_MouseRightButtonDown);

            if (this.AssociatedObject is Button)
            {
                (AssociatedObject as Button).Click -= new RoutedEventHandler(AssociatedObject_Click);
            }
            else if (this.AssociatedObject is HyperlinkButton)
            {
                (AssociatedObject as HyperlinkButton).Click -= new RoutedEventHandler(AssociatedObject_Click);
            }
        }
    }
}
