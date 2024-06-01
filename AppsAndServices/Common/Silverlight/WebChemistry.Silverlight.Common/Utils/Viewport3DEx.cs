using ImageTools;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using WebChemistry.Framework.Controls;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Silverlight.Common
{
    public static class Viewport3DEx
    {
        /// <summary>
        /// Render the viewport contents to Png.
        /// </summary>
        /// <param name="vp"></param>
        public static async void RenderToPng(this Viewport3D vp)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png"
            };

            if (sfd.ShowDialog() == true)
            {

                var cs = ComputationService.Default;
                var progress = cs.Start();
                cs.TimerVisibility = System.Windows.Visibility.Collapsed;
                try
                {
                    progress.UpdateIsIndeterminate(true);
                    progress.UpdateCanCancel(false);
                    progress.UpdateStatus("Rendering...");

                    await TaskEx.Delay(TimeSpan.FromSeconds(0.2));

                    int w = 1600, h = 1200;

                    var image = vp.RenderToBitmap(w, h);
                    var ret = image.ToImage();

                    using (var stream = sfd.OpenFile())
                    {
                        ret.WriteToStream(stream, sfd.SafeFileName);
                    }

                    LogService.Default.Message("Image saved.");
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
