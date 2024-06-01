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
    public partial class CorrelationControl : UserControl
    {
        CorrelationViewModel vm;
        LinearAxis xAxis, yAxis;

        public CorrelationControl()
        {
            InitializeComponent();


            yAxis = new LinearAxis(AxisPosition.Left) { MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, TitleFontWeight = OxyPlot.FontWeights.Bold, TitleFontSize = 16 };
            xAxis = new LinearAxis(AxisPosition.Bottom) { MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.Dot, TitleFontWeight = OxyPlot.FontWeights.Bold, TitleFontSize = 16 };
                        
            try
            {
                vm = ServiceLocator.Current.GetInstance<CorrelationViewModel>();

                vm.ObservePropertyChanged(this, (l, m, p) =>
                    {
                        if (p == "CurrentCorrelation") l.UpdateCurrent();
                    });

            }
            catch
            {
            }

            DrawEmpty();
        }

        Random rnd = new Random();
        void DrawEmpty()
        {
            PlotModel model = new PlotModel("Random stuff");
            model.Subtitle = "Nothing else is selected.";
            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);

            xAxis.Minimum = yAxis.Minimum = -1;
            xAxis.Maximum = yAxis.Maximum = 1;

            xAxis.Title = "X";
            yAxis.Title = "Y";

            var degree = (15 + rnd.Next(35));
            var delta = Math.PI / 2;

            model.Series.Add(new LineSeries
            {
                IsSelected = false,
                Points = Enumerable.Range(0, 1000).Select(i =>
                {
                    var t = 2 * i * Math.PI / 999;
                    return (IDataPoint)new DataPoint
                    {
                        X = Math.Sin((degree) * t + delta),
                        Y = Math.Sin((degree - 1) * t)
                    };
                }).ToArray()
            });

            plot.Model = model;
        }

        Correlation currentCorrelation;

        const int MaxDataPoints = 10000;

        void UpdateCurrent()
        {
            var correlation = vm.CurrentCorrelation;
            currentCorrelation = correlation;

            if (correlation == null)
            {
                DrawEmpty();
                return;
            }
            
            PlotModel model = new PlotModel(string.Format("{0} ({1})", correlation.Structure.Structure.Id, correlation.PartitionName));
            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Subtitle = string.Format("# = {0}, R² = {1}, ρ = {2}, δ² = {3}, D = {4} (avg. {5})",                 
                correlation.DataPoints.Count, // 0
                correlation.FormattedPearsonCoefficient, // 1
                correlation.FormattedSpearmanCoefficient, // 2
                correlation.FormattedRmsd, // 3
                correlation.FormattedAbsoluteDifferenceSum, // 4
                (correlation.AbsoluteDifferenceSum / correlation.DataPoints.Count).ToStringInvariant("0.000") // 5
                );

            if (displayAll.IsChecked == false && correlation.DataPoints.Count > MaxDataPoints)
            {
                LogService.Default.Info("Too many data points ({0}), displaying {1} randomly selected ones (export to CSV and use Excel or something to display everything, or check 'Display All Data' and suffer the consequences).", 
                    correlation.DataPoints.Count, MaxDataPoints);
            }

            xAxis.Title = correlation.IndependentName;
            yAxis.Title = correlation.DependentName;
            model.Series.Add(new ScatterSeries { 
                MarkerType = OxyPlot.MarkerType.Circle,
                MarkerStrokeThickness = 1,
                TrackerKey = "atomTracker",
                ItemsSource = displayAll.IsChecked == true
                    ? correlation.DataPoints
                    : correlation.DataPoints.ToRandomlyOrderedArray().Take(MaxDataPoints).ToArray(),
                Mapping = x => new DataPoint(((Correlation.AtomDataPoint)x).X, ((Correlation.AtomDataPoint)x).Y)
                //Points = correlation.DataPoints.ToRandomlyOrderedArray().Take(MaxDataPoints).ToArray()
            });
            model.Series.Add(new LineSeries
            {
                IsSelected = false,
                Points = new IDataPoint[]
                { 
                    new DataPoint { X = correlation.IndependentRange.Minimum, Y = correlation.IndependentRange.Minimum * correlation.A + correlation.B },
                    new DataPoint { X = correlation.IndependentRange.Maximum, Y = correlation.IndependentRange.Maximum * correlation.A + correlation.B }
                }
            });
            
            plot.Model = model;

            UpdateRanges();
        }

        void UpdateRanges()
        {
            if (UserRangeCheckbox.IsChecked == true)
            {
                var q = from minX in MinXText.Text.ToDouble()
                        from minY in MinYText.Text.ToDouble()
                        from maxX in MaxXText.Text.ToDouble()
                        from maxY in MaxYText.Text.ToDouble()
                        select Tuple.Create(minX, maxX, minY, maxY);
                
                if (q.IsSomething())
                {
                    var val = q.GetValue();
                    var Y = yAxis;
                    Y.Minimum = val.Item3;
                    Y.Maximum = val.Item4;

                    var X = xAxis;
                    X.Minimum = val.Item1;
                    X.Maximum = val.Item2;
                }
                else
                {
                    LogService.Default.Error("Axis Ranges", "One or more ranges are in an invalid format.");
                }
            }
            else
            {
                if (currentCorrelation == null)
                {
                    var Y = yAxis; //CorrelationChart.Axes[0] as LinearAxis;
                    Y.Minimum = -1;
                    Y.Maximum = 1;

                    var X = xAxis; //CorrelationChart.Axes[1] as LinearAxis;
                    X.Minimum = -1;
                    X.Maximum = 1;
                }
                else
                {
                    double minX = currentCorrelation.IndependentRange.Minimum * 1.1;
                    double maxX = currentCorrelation.IndependentRange.Maximum * 1.1;

                    double minLineY = (currentCorrelation.IndependentRange.Minimum * currentCorrelation.A + currentCorrelation.B) * 1.1;
                    double maxLineY = (currentCorrelation.IndependentRange.Maximum * currentCorrelation.A + currentCorrelation.B) * 1.1;

                    double minY = currentCorrelation.DependentRange.Minimum * 1.1;
                    double maxY = currentCorrelation.DependentRange.Maximum * 1.1;

                    var Y = yAxis;// CorrelationChart.Axes[0] as LinearAxis;
                    Y.Minimum = Math.Min(minY, minLineY);
                    Y.Maximum = Math.Max(maxY, maxLineY);

                    var X = xAxis;// CorrelationChart.Axes[1] as LinearAxis;
                    X.Minimum = minX;
                    X.Maximum = maxX;
                }
            }

            plot.ResetAllAxes();
        }

        private void ApplyDisplayAll(object sender, RoutedEventArgs e)
        {
            UpdateCurrent();
        }

        private void ApplyUserRangesChecked(object sender, RoutedEventArgs e)
        {
            var Y = yAxis;
            MinYText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Y.Minimum);
            MaxYText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Y.Maximum);

            var X = xAxis;
            MinXText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", X.Minimum);
            MaxXText.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", X.Maximum);

            UpdateRanges();
        }

        private void ApplyUserRanges(object sender, RoutedEventArgs e)
        {
            UpdateRanges();
        }
        
        WriteableBitmap RenderToBitmap()
        {
            var height = PlotWrap.ActualHeight;
            var width = PlotWrap.ActualWidth;

            WriteableBitmap bmp = new WriteableBitmap((int)width, (int)height);
            //var t = new MatrixTransform { Matrix = Matrix.Identity };
            //var t = new ScaleTransform { ScaleX = (double)w / width, ScaleY = (double)h / height };
            bmp.Render(PlotWrap, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });

            return bmp;        
        }


        private async void SaveAll(object sender, System.Windows.RoutedEventArgs e)
        {
            //if (MessageBox.Show("This operation can be very slow and take some time depending on how many sets have you computed." + Environment.NewLine + Environment.NewLine +
            //    "During the export the application will be non-responsive and you browser might temporarily freeze." + Environment.NewLine + Environment.NewLine +
            //    "Do you want to continue?", "Prompt", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            //{
            //    return;
            //}

            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "ZIP File (*.zip)|*.zip"
            };

            var cs = ComputationService.Default;
            var tempCorr = vm.CurrentCorrelation;
            try
            {
                if (sfd.ShowDialog() == true)
                {
                    var progress = cs.Start();

                    var session = ServiceLocator.Current.GetInstance<Session>();
                    
                    var maxProgress = session.Structures.Sum(c => c.Correlations.Count);
                    int count = 0;

                    progress.Update(isIndeterminate: true, canCancel: false, statusText: "Exporting...", currentProgress: 0, maxProgress: maxProgress);

                    await TaskEx.Delay(100);

                    using (var stream = sfd.OpenFile())
                    using (var zip = new ZipOutputStream(stream))
                    {
                        foreach (var s in session.Structures.Skip(1))
                        {
                            foreach (var c in s.Correlations.SelectMany(c => c.Value))
                            {
                                zip.PutNextEntry(new ZipEntry(s.Structure.Id + "\\" + c.IndependentName + "_" + c.DependentName + "_" + c.PartitionName + ".png"));
                                vm.CurrentCorrelation = c;
                                plot.InvalidateMeasure();
                                plot.InvalidateArrange();
                                //OxyPlot.Pdf.PdfExporter.Export(plot.Model, stream, 1024, 1024);           
                                var image = RenderToBitmap().ToImage();
                                image.WriteToStream(zip);
                                //zip.CloseEntry();
                                progress.Update(currentProgress: ++count);
                               // await TaskEx.Delay(100);
                            }
                        }
                    }
                }

                LogService.Default.Message("Export successful.");
            }
            catch (Exception ex)
            {
                LogService.Default.Error("Export", ex.Message);
            }
            finally
            {
                cs.End();
                vm.CurrentCorrelation = tempCorr;
            }
        }

        private void SaveImage(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png"
            };

            try
            {
                if (sfd.ShowDialog() == true)
                {
                    var image = RenderToBitmap().ToImage();

                    using (var stream = sfd.OpenFile())
                    {
                        
                        //OxyPlot.Pdf.PdfExporter.Export(plot.Model, stream, 1024, 1024);           
                        image.WriteToStream(stream);
                    }
                }

                LogService.Default.Message("Image saved.");
            }
            catch (Exception ex)
            {
                LogService.Default.Error("Saving Image", ex.Message);
            }
            finally
            {

            }
        }

        private void PlotWrap_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            plot.Width = plot.ActualHeight;
        }
    }
}
