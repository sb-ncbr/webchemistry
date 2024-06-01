using ICSharpCode.SharpZipLib.Zip;
using ImageTools;
using Microsoft.Practices.ServiceLocation;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Charges.Silverlight.ViewModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class AggregateControl : UserControl
    {
        AggregateViewModel vm;

        public AggregateControl()
        {
            InitializeComponent();

            try
            {
                vm = ServiceLocator.Current.GetInstance<AggregateViewModel>();
                vm.Updated.Subscribe(_ => Update());
            }
            catch
            {
            }

            DrawEmpty();
        }

        string GetPropertySelectorName()
        {
            switch (vm.PropertyTypeIndex)
            {
                case 0: return "MinCharge";
                case 1: return "MaxCharge";
                case 2: return "Average";
                case 3: return "AbsAverage";
                case 4: return "Sigma";
                case 5: return "AbsSigma";
                default: return "Count";
            }
        }

        void DrawEmpty()
        {
            var model = new PlotModel("No Data");
            plot.Model = model;
        }

        void Update()
        {
            if (vm.Charges == null)
            {
                DrawEmpty();
                return;
            }

            var selected = vm.Charges.Where(c => c.IsSelected).ToArray();

            if (selected.Length == 0 || selected[0].CurrentCluster == null)
            {
                DrawEmpty();
                return;
            }

            var pivot = selected[0].CurrentCluster.Clustering;


            var selector = GetPropertySelectorName();

            PlotModel model = new PlotModel(string.Format("{0} ({1})", vm.AnalyzeViewModel.CurrentStructure.Structure.Id, vm.AnalyzeViewModel.CurrentPartition))
            {
                Subtitle = string.Format("Aggregate: {0}, Property: {1}", vm.CurrentAggregateName, vm.PropertyTypes[vm.PropertyTypeIndex]),
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendFontSize = 14,
                LegendFontWeight = 800
            };

            var cats = pivot.Clusters.Select(c => c.Key).OrderBy(c => c).ToArray();
            var axis = new CategoryAxis(AxisPosition.Bottom, title: null, categories: cats) 
            { 
                FontWeight = 800,
                FontSize = 14,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            model.Axes.Add(axis);
            model.Axes.Add(new LinearAxis(AxisPosition.Left) { MinimumPadding = 0.05, MaximumPadding = 0.05, MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot });

            foreach (var charges in selected)
            {

                var series = new ColumnSeries
                {
                    Title = charges.Charges.Name,
                    TrackerKey = "dataTracker",
                    FillColor = OxyColor.FromRgb(charges.Charges.Color.R, charges.Charges.Color.G, charges.Charges.Color.B),
                    ItemsSource = charges.CurrentCluster.Stats.OrderBy(s => s.Key).ToArray(),
                    ValueField = selector
                };
                model.Series.Add(series);
            }
            
            plot.Model = model;
        }

        WriteableBitmap RenderToBitmap()
        {
            var height = PlotWrap.ActualHeight;
            var width = PlotWrap.ActualWidth;

            WriteableBitmap bmp = new WriteableBitmap((int)width, (int)height);
            //var t = new MatrixTransform { Matrix = Matrix.Identity };
            //var t = new ScaleTransform { ScaleX = (double)w / width, ScaleY = (double)h / height };
            bmp.Render(PlotWrap, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });


            ////var cc = new Canvas() { Width = width, Height = height };
            ////var rc = new OxyPlot.Silverlight.SilverlightRenderContext(cc) { RendersToScreen = false };
            ////plot.Model.Render(rc, width, height);
            ////cc.InvalidateMeasure();
            ////cc.InvalidateArrange();
            ////bmp.Render(cc, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });

//            SvgExporter.Export()


            return bmp;
        }

        private void SaveImage(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png"
                //|SVG (*.svg)|*.svg"
            };

            try
            {
                if (sfd.ShowDialog() == true)
                {
                    using (var stream = sfd.OpenFile())
                    {                        
                        switch (sfd.FilterIndex)
                        {
                            case 1:
                                var image = RenderToBitmap().ToImage();
                                image.WriteToStream(stream);
                                LogService.Default.Message("Image saved.");
                                //plot.Model.Render();
                                break;
                            //case 2:
                            //    var rc = new OxyPlot.Silverlight.SilverlightRenderContext(new Canvas());
                            //    SvgExporter.Export(plot.ActualModel, stream, plot.ActualWidth, plot.ActualHeight, true, rc);
                            //    break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Default.Error("Saving Image", ex.Message);
            }
            finally
            {

            }
        }
    }
}
