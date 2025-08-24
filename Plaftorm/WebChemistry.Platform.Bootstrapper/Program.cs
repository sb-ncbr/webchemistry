using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.MotiveValidator.Database;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Services;

namespace WebChemistry.Platform.Bootstrapper
{
    class Program
    {
        static StringBuilder LogText = new StringBuilder();

        static void Log(string format, params object[] args)
        {
            var ts = DateTime.UtcNow;
            var inner = string.Format(format, args);
            var msg = string.Format("[{0}] {1}", ts, inner);
            LogText.AppendLine(msg);

            if (msg.Length < 80) Console.WriteLine(msg);
            else
            {
                Console.WriteLine("[{0}]", ts);
                Console.WriteLine(ServiceHelpGenerator.MakeLines(inner, 2));
            }
        }

        static void Main(string[] args)
        {
            //var cfg = @"C:\Projects\WebChemistry\Plaftorm\bin\Bootstrapper\config.cfg";

            var timer = Stopwatch.StartNew();
            Log("Started.", DateTime.UtcNow);
            string logFolder = null;
            try
            {
                var input = XElement.Load(args[0]);
                logFolder = input.Attribute("LogFolder") != null ? input.Attribute("LogFolder").Value : null;
                ServerManager.Init(input.Element("ServerConfig").Value);

                if (input.Element("Services") != null) UpdateServices(input.Element("Services"));
                if (input.Element("Databases") != null) UpdateDatabases(input.Element("Databases"));
                if (input.Element("UpdateMasterDatabasesIndex") != null) UpdateMasterDatabasesIndex();
                if (input.Element("MotiveValidatorLigandNames") != null) UpdateValidator(input.Element("MotiveValidatorLigandNames"));
                if (input.Element("ValidatorDbUpdate") != null) UpdateValidatorDb();

                timer.Stop();
            }
            catch (Exception e)
            {
                Log("Fatal error: {0}", e.Message);
            }
            Log("Finished. {1} elapsed.", DateTime.UtcNow, timer.Elapsed);

            try
            {
                if (logFolder != null) SaveLog(logFolder);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to save the log file: {0}", e.Message);
            }
        }

        static void SaveLog(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var time = DateTime.UtcNow;
            var fn = Path.Combine(path, string.Format("update_{0}_{1}_{2}-{3}_{4}.txt", time.Year, time.Month, time.Day, time.Hour, time.Minute));
            File.WriteAllText(fn, LogText.ToString());
        }

        static void UpdateDatabases(XElement input)
        {
            MasterServer master = ServerManager.MasterServer;

            Log("Updating Databases");
            foreach (var database in input.Elements())
            {
                try
                {
                    Log("Updating database '{0}'.", database.Attribute("Name").Value);

                    DatabaseInfo db = master.PublicDatabases.Exists(database.Attribute("Name").Value)
                        ? (DatabaseInfo)master.PublicDatabases.GetDatabaseByName(database.Attribute("Name").Value)
                        : (DatabaseInfo)master.PublicDatabases.CreateDatabase(database.Attribute("Name").Value, description: database.Attribute("Description").Value,
                        customId: database.Attribute("Id").Value);

                    int progress = 0;
                    var src = database.Attribute("Source").Value;
                    var total = Directory.EnumerateFiles(src).Count();
                    Log("'{0}': Found {1} input entries.", db.Name, total);
                    var result = db.UpdateDatabase(src, visitedCallback: _ =>
                    {
                        progress++;
                        if (progress % 10000 == 0 || progress == total) Log("'{0}': Visited {1}/{2} entries.", db.Name, progress, total);
                    });

                    // force the view.
                    db.DefaultView.Snapshot();

                    Log("'{0}': Add {1} Rem {2} Mod {3} Err {4}.", db.Name, result.NumAdded, result.NumRemoved, result.NumModified, result.NumError);
                }
                catch (Exception e)
                {
                    Log("'{0}' - Error: {1}", database.Attribute("Name").Value, e.Message);
                }
            }
        }


        static void UpdateMasterDatabasesIndex()
        {
            MasterServer master = ServerManager.MasterServer;

            Log("Updating Databases");
            foreach (var database in master.PublicDatabases.GetAll())
            {
                try
                {
                    Log("Updating database index '{0}'.", database.Name);
                    
                    int progress = 0;
                    var total = database.GetStatistics().MoleculeCount;
                    Log("'{0}': Found {1} entries to update.", database.Name, total);
                    var result = database.UpdateDatabaseIndex(visitedCallback: _ =>
                    {
                        progress++;
                        if (progress % 10000 == 0 || progress == total) Log("'{0}': Visited {1}/{2} entries.", database.Name, progress, total);
                    });

                    // force the view.
                    database.DefaultView.Snapshot();

                    Log("'{0}': Updated {1} Err {2}.", database.Name, result.NumUpdated, result.NumError);
                }
                catch (Exception e)
                {
                    Log("'{0}' - Error: {1}", database.Name, e.Message);
                }
            }
        }

        static void UpdateServices(XElement input)
        {
            var server = ServerManager.MasterServer;

            Log("Updating Services");

            foreach (var svc in input.Elements())
            {
                var name = svc.Attribute("Name").Value;
                try
                {
                    var si = server.Services.RegisterOrUpdateService(
                        name,
                        svc.Attribute("Executable").Value,
                        Computation.ComputationPriority.Default,
                        svc.Attribute("Path").Value);
                    Log("{0}: Updated to version '{1}'.", name, si.CurrentVersion);
                }
                catch (Exception e)
                {
                    Log("{0}: [Error] {1}", name, e.Message);
                }
            }
        }

        static void UpdateValidator(XElement e)
        {
            Log("Updating MotiveValidator");
            try
            {
                var csv = e.Attribute("Source").Value;

                if (!File.Exists(csv))
                {
                    Log("Could not find ligexp CSV for MotiveValidator.");
                    return;
                }

                var minVersion = AppVersion.Parse(e.Attribute("MinVersion").Value);
                var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");
                var toUpdate = svc.Versions.Where(v => v.Key.CompareTo(minVersion) >= 0).Select(v => v.Key).ToArray();
                foreach (var v in toUpdate)
                {
                    var target = Path.Combine(svc.GetServicePath(v), "ligandexpo.csv");
                    File.Copy(csv, target, overwrite: true);
                }
                Log("MotiveValidator ligand expo names updated for {0} versions.", toUpdate.Length);
            }
            catch (Exception ex)
            {
                Log("MotiveValidator ligand expo name update failed: {0}", ex.Message);
            }
        }

        static void UpdateValidatorDb()
        {
            Log("Updating ValidatorDB");

            try
            {
                var app = ServerManager.GetAppServer("Atlas").GetOrCreateApp<MotiveValidatorDatabaseApp>("ValidatorDb", server =>
                {
                    return MotiveValidatorDatabaseApp.Create("ValidatorDb", server);
                });
                if (app == null)
                {
                    Log("ValidatorDB app not found.");
                    return;
                }
                var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");

                var appPath = app.GetBasePath();
                var cfgPath = Path.Combine(appPath, "mvconfig.json");

                var dirs = Directory.EnumerateDirectories(appPath)
                    .Select(d => new DirectoryInfo(d).Name.ToInt())
                    .Flatten()
                    .ToList();                
                string id = dirs.Count == 0 ? "1" : (dirs.Max() + 1).ToString();

                var dbPath = Path.Combine(appPath, id);
                Directory.CreateDirectory(dbPath);
                var compPath = Path.Combine(dbPath, "computation");
                
                string args = string.Format("\"{0} \" \"{1} \"", compPath, cfgPath);
                var pi =
                    Process.Start(new ProcessStartInfo(svc.GetExecutablePath(), args)
                    {
                        //CreateNoWindow = true,
                        //WindowStyle = ProcessWindowStyle.Hidden
                    });
                Log("ValidatorDB: Started MV process. Id = {0}", id);
                pi.WaitForExit();
                Log("ValidatorDB: MV process finished.");

                var statsJson = Path.Combine(compPath, "result", "stats.json");
                if (!File.Exists(statsJson))
                {
                    Log("ValidatorDB: Failed (stats file not found).");
                    return;
                }
                ////var stats = JsonConvert.DeserializeAnonymousType(File.ReadAllText(statsJson), new { MotiveCount = 1, StructureCount = 1, ModelCount = 1 });
                ////app.AnalyzedModelCount = stats.ModelCount;
                ////app.AnalyzedStructureCount = stats.StructureCount;
                app.LastUpdated = DateTimeService.GetCurrentTime();
                app.DatabaseVersionId = id;
                app.Store();
                Log("ValidatorDB: Done.");
            }
            catch (Exception ex)
            {
                Log("ValidatorDB update failed: {0}", ex.Message);
            }
        }
    }
}
