using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using WebChemistry.Framework.Core;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Services;
using WebChemistry.Platform.Users;

namespace WebChemistry.Web.Helpers
{
    public abstract class AppControllerBase : Controller
    {
        protected void Schedule(ComputationInfo comp)
        {
            comp.Schedule();
            var scheduler = ServerManager.MasterServer.ComputationScheduler;
            scheduler.Update();
            var state = comp.GetStatus().State;
            if (state == ComputationState.New)
            {
                throw new InvalidOperationException("Concurrent computation limit reached. Please try again later.");
            }
        }

        protected ActionResult DownloadService(ServiceInfo svc, string svcVersion, string downloadName)
        {
            try
            {
                AppVersion version = string.IsNullOrEmpty(svcVersion) ? svc.CurrentVersion : AppVersion.Parse(svcVersion);
                var path = svc.GetVersionPath(version);

                if (!Directory.Exists(path))
                {
                    return HttpNotFound(string.Format("Version '{0}' not found.", svcVersion));
                }

                byte[] bytes = null;
                using (var byteStream = new MemoryStream())
                using (var zip = new ZipOutputStream(byteStream))
                {
                    zip.UseZip64 = UseZip64.Off;
                    byte[] buffer = new byte[4096];
                    foreach (var f in Directory.GetFiles(path))
                    {
                        zip.PutNextEntry(new ZipEntry(Path.GetFileName(f)));
                        using (FileStream fs = System.IO.File.OpenRead(f))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                zip.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                        zip.CloseEntry();
                    }
                    zip.Flush();
                    zip.Finish();

                    bytes = new byte[byteStream.Length];
                    Array.Copy(byteStream.GetBuffer(), bytes, byteStream.Length);
                }
                return File(bytes, "application/octet-stream", downloadName + "_" + version.ToString() + ".zip");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error creating the service package: " + e.Message);
            }
        }

        /// <summary>
        /// supported types: csv/none
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected ActionResult ListComputations(string type, UserInfo user, string compName)
        {
            if (string.IsNullOrWhiteSpace(type) || !type.EqualOrdinalIgnoreCase("csv"))
            {
                var xs = user.Computations.GetAll()
                    .Where(c => c.Name.EqualOrdinal(compName))
                    .GroupBy(c => new DateTime(c.DateCreated.Year, c.DateCreated.Month, c.DateCreated.Day))
                    .OrderByDescending(g => g.Key)
                    .Select(g => Tuple.Create(g.Key, g.OrderByDescending(c => c.DateCreated).ToArray()))
                    .ToArray();
                ViewBag.ComputationsByDay = xs;
                ViewBag.TotalCount = xs.Sum(x => x.Item2.Length);
                return View("ComputationsList");
            }
            else
            {
                var csv = user.Computations.GetAll()
                    .Where(c => c.Name.EqualOrdinal(compName))
                    .OrderByDescending(c => c.DateCreated).GetExporter()
                    .AddStringColumn(c => c.DateCreated, "Date")
                    .AddStringColumn(c => c.Source, "Source")
                    .AddStringColumn(c => c.ShortId, "Id")
                    .ToCsvString();
                return ActionHelper.TextContent(csv);
            }
        }
    }
}
