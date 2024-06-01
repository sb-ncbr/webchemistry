using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ImageTools;
using WebChemistry.SiteBinder.Silverlight.Visuals;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using System.Threading.Tasks;
using System.Globalization;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Framework.Core;

namespace WebChemistry.SiteBinder.Silverlight.Controls
{
    public partial class VisualizationControl : UserControl
    {
        //WebChemistry.Framework.Controls.Viewport3D viewport;

        public VisualizationControl()
        {
            InitializeComponent();

            ModeCombo.SelectionChanged += (_, __) =>
                {
                    var visual = viewport.Visual as MultiMotiveVisual3D;
                    if (visual == null) return;

                    else if (ModeCombo.SelectedIndex == 0) { visual.RenderMode = RenderMode.Wireframe; ColorsCombo.Visibility = System.Windows.Visibility.Visible; }
                    if (ModeCombo.SelectedIndex == 1) { visual.RenderMode = RenderMode.BallsAndSticks; ColorsCombo.Visibility = System.Windows.Visibility.Visible; }
                    else if (ModeCombo.SelectedIndex == 2) { visual.RenderMode = RenderMode.Sticks; ColorsCombo.Visibility = System.Windows.Visibility.Collapsed; }
                    //else if (ModeCombo.SelectedIndex == 3) { visual.RenderMode = RenderMode.Nothing; ColorsCombo.Visibility = System.Windows.Visibility.Collapsed; }
                    viewport.Render();
                };

            ColorsCombo.SelectionChanged += (_, __) =>
            {
                var visual = viewport.Visual as MultiMotiveVisual3D;
                if (visual == null) return;

                if (ColorsCombo.SelectedIndex == 0) visual.AtomColorMode = AtomColorMode.Structure;
                else if (ColorsCombo.SelectedIndex == 1) visual.AtomColorMode = AtomColorMode.Element;
                else if (ColorsCombo.SelectedIndex == 2) MessageBox.Show("Not implemented.");  //visual.AtomColorMode = AtomColorMode.Element;
                viewport.Render();
            };

            DisplayCountCombo.SelectionChanged += (_, __) =>
            {
                var visual = viewport.Visual as MultiMotiveVisual3D;
                if (visual == null) return;

                if (DisplayCountCombo.SelectedIndex == 0) visual.MaxVisuals = 0;
                else if (DisplayCountCombo.SelectedIndex == 1) visual.MaxVisuals = 1;
                else if (DisplayCountCombo.SelectedIndex == 2) visual.MaxVisuals = 5;
                else if (DisplayCountCombo.SelectedIndex == 3) visual.MaxVisuals = 10;
                else if (DisplayCountCombo.SelectedIndex == 4) visual.MaxVisuals = 25;
                else if (DisplayCountCombo.SelectedIndex == 5) visual.MaxVisuals = 50;
                else if (DisplayCountCombo.SelectedIndex == 6) visual.MaxVisuals = 100;
                viewport.Render();
            };

            DisplayCombo.SelectionChanged += (_, __) =>
            {
                var visual = viewport.Visual as MultiMotiveVisual3D;
                if (visual == null) return;

                if (DisplayCombo.SelectedIndex == 0) visual.AtomDisplayMode = AtomDisplayMode.All;
                else if (DisplayCombo.SelectedIndex == 1) visual.AtomDisplayMode = AtomDisplayMode.Selection;
                viewport.Render();
            };

            ColorsCombo.Visibility = System.Windows.Visibility.Visible;

            DisplayCountCombo.Items.Add("Show None");
            DisplayCountCombo.Items.Add("Show 1");
            DisplayCountCombo.Items.Add("Show 5");
            DisplayCountCombo.Items.Add("Show 10");
            DisplayCountCombo.Items.Add("Show 25");
            DisplayCountCombo.Items.Add("Show 50");
            DisplayCountCombo.Items.Add("Show 100");
            DisplayCountCombo.SelectedIndex = 4;

            ModeCombo.Items.Add("Wireframe");
            ModeCombo.Items.Add("Balls and Sticks");
            ModeCombo.Items.Add("Sticks");
            //ModeCombo.Items.Add("Nothing");
            ModeCombo.SelectedIndex = 0;

            ColorsCombo.Items.Add("Color by Selection");
            ColorsCombo.Items.Add("Color by Element");
            //ColorsCombo.Items.Add("Color by Charges");
            ColorsCombo.SelectedIndex = 0;

            DisplayCombo.Items.Add("Display All Atoms");
            DisplayCombo.Items.Add("Display Selected Atoms");
            DisplayCombo.SelectedIndex = 0;

            //viewport = new Framework.Controls.Viewport3D();
            //LayoutRoot.Children.Add(viewport);
            LayoutRoot.MouseEnter += (_, __) =>
            {
                if (viewport.Visual is MultiMotiveVisual3D)
                {
                    (viewport.Visual as MultiMotiveVisual3D).SuppressTooltip = false;
                }
            };

            LayoutRoot.MouseLeave += (_, __) =>
            {
                if (viewport.Visual is MultiMotiveVisual3D)
                {
                    (viewport.Visual as MultiMotiveVisual3D).SuppressTooltip = true;
                }
            };

            LayoutRoot.MouseEnter += (_, __) =>
            {
                if (viewport.Visual is MultiMotiveVisual3D)
                {
                    (viewport.Visual as MultiMotiveVisual3D).SuppressTooltip = false;
                }
            };
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            viewport.RenderToPng();
        }

        private async void Render(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png"
            };

            if (sfd.ShowDialog() == true)
            {
                var session = ServiceLocator.Current.GetInstance<Session>();

                if (session.SelectedStructures.Count > 1000)
                {
                    var message = "You are attempring to render a large number of structures. This operation is very memory consuming and might take long time (about 60s per 1000 structures).\n\n" +
                        "The only way to cancel this operation is to kill the browser process.\n\n" +
                        "Do you want to proceed?";

                    if (MessageBox.Show(message, "Confirmation", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;
                }

                var cs = ComputationService.Default;
                var progress = cs.Start();
                cs.TimerVisibility = System.Windows.Visibility.Collapsed;
                try
                {
                    progress.UpdateIsIndeterminate(true);
                    progress.UpdateCanCancel(false);

                    var time = TimeSpan.FromSeconds((int)((double)session.SelectedStructures.Count / 1000.0 * 60));
                    progress.UpdateStatus(string.Format("Rendering... this might take some time (about {0} minutes) and the browser will freeze during the process.", Math.Ceiling(time.TotalMinutes)));

                    await TaskEx.Delay(TimeSpan.FromSeconds(0.2));

                    var started = DateTime.Now;
                    var visual = viewport.Visual as MultiMotiveVisual3D;

                    var oldzoom = viewport.Viewport.Camera.Radius;

                    visual.RemoveAll();
                    visual.AddOffscreen(session.SelectedStructures);
                    viewport.Viewport.RenderSynchronously();
                    viewport.InvalidateMeasure();
                    viewport.InvalidateArrange();
                    viewport.UpdateLayout();

                    int w = 1600, h = 1200;

                    var image = viewport.RenderToBitmap(w, h);
                    var ret = image.ToImage();
                    
                    visual.RemoveAll();
                    visual.Add(session.SelectedStructures);
                    viewport.Viewport.Camera.Radius = oldzoom;
                    viewport.Render();


                    using (var stream = sfd.OpenFile())
                    {
                        ret.WriteToStream(stream);
                    }

                    image = null;
                    ret = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    LogService.Default.Message("Image saved in {0}s.", ComputationService.GetElapasedTimeString(DateTime.Now - started));
                }
                catch (Exception ex)
                {
                    LogService.Default.Error("Saving Image", ex.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }
    }
}
