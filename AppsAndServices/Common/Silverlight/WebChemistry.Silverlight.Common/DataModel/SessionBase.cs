using GalaSoft.MvvmLight.Command;
using ICSharpCode.SharpZipLib.Zip;
using Ninject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Silverlight.Common.DataModel
{
    /// <summary>
    /// Used to obtain current session using the service locator.
    /// </summary>
    public interface ISession
    {

    }

    /// <summary>
    /// Base of the session object.
    /// </summary>
    /// <typeparam name="TStructure"></typeparam>
    public abstract class SessionBase<TStructure> : ObservableObject, ISession
        where TStructure : StructureWrapBase<TStructure>, new()
    {
        const string WorkspaceFilename = "_workspace.xml";
        const string QueryHistoryFilename = "_qhistory.xml";

        public readonly string Name, WorkspaceExtension;

        /// <summary>
        /// The log.
        /// </summary>
        [Inject]
        public LogService Log { get; set; }

        /// <summary>
        /// Structure map.
        /// </summary>
        public Dictionary<string, TStructure> StructureMap = new Dictionary<string, TStructure>(StringComparer.OrdinalIgnoreCase);

        private ObservableCollection<TStructure> _structures;
        /// <summary>
        /// Structures in this session.
        /// </summary>
        public ObservableCollection<TStructure> Structures
        {
            get
            {
                return _structures;
            }

            set
            {
                if (_structures == value) return;

                _structures = value;
                NotifyPropertyChanged("Structures");
            }
        }
        
        private PagedCollectionView _structuresView;
        /// <summary>
        /// View of the structures.
        /// </summary>
        public PagedCollectionView StructuresView
        {
            get
            {
                return _structuresView;
            }

            set
            {
                if (_structuresView == value) return;

                _structuresView = value;
                NotifyPropertyChanged("StructuresView");
            }
        }
        
        /// <summary>
        /// Update the view.
        /// </summary>
        public abstract void UpdateStructuresView();
        
        /// <summary>
        /// What to do when the descriptors updated.
        /// </summary>
        public virtual void CurrentDescriptorUpdated()
        {

        }

        
        /// <summary>
        /// Query context.
        /// </summary>
        public ExecutionContext QueryExecutionContext { get; private set; }

        /// <summary>
        /// Scripting object.
        /// </summary>
        internal QueriesScripting QueriesScripting { get; private set; }

        /// <summary>
        /// Misc scripting functions.
        /// </summary>
        internal UtilsScripting UtilsScripting { get; private set; }

        /// <summary>
        /// Descriptors model.
        /// </summary>
        public StructureDescriptorsModel<TStructure> Descriptors { get; private set; }

        private string _queryString;
        /// <summary>
        /// Current query string.
        /// </summary>
        public string QueryString
        {
            get
            {
                return _queryString;
            }

            set
            {
                if (_queryString == value) return;

                _queryString = value;
                NotifyPropertyChanged("QueryString");
            }
        }

        private ICommand _executeQueryCommand;
        public ICommand ExecuteQueryCommand
        {
            get
            {
                _executeQueryCommand = _executeQueryCommand ?? new RelayCommand<string>(cmd => QueryService.Default.Execute(this, QueryString, cmd != null && cmd.EqualOrdinalIgnoreCase("export")));
                return _executeQueryCommand;
            }
        }

        /// <summary>
        /// Shows the warnings for currently loaded structures.
        /// </summary>
        /// <returns></returns>
        public string ShowWarnings()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in Structures.OrderBy(s => s.Structure.Id))
            {
                var ws = s.Warnings;
                var c = ws.Count();
                if (c == 0) continue;

                sb.AppendLine(string.Format("{0} ({1}):", s.Structure.Id, c));
                foreach (var w in ws) sb.AppendLine("  " + w);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Opens structures using an OpenFileDialog.
        /// </summary>
        public async void OpenStructures()
        {
            var filterPretty = string.Join(", ", StructureReader.SupportedExtensions.Select(e => "*" + e));
            var filter = string.Join(";", StructureReader.SupportedExtensions.Select(e => "*" + e));
            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = string.Format("Structures ({0})|{1}|Properties (*{2})|*{2}|ZIP Files (*.zip)|*.zip", filterPretty, filter, AtomPropertiesEx.DefaultExtension)
                    //"All Structures (*.pdb, *.mol, *.mdl, *.sdf, *.sd, *.ml2, *.mol2)|*.pdb;*.mol;*.mdl;*.sdf;*.sd;*.ml2;*.mol2|ZIP Files (*.zip)|*.zip"
            };

            int fileCount = 0;

            try
            {
                if (ofd.ShowDialog() == true)
                {
                    fileCount = ofd.Files.Count();
                    await Load(ofd.Files);
                }
            }
            catch (Exception e)
            {
                Log.Error("Opening Files", e.Message);
                if (fileCount > 1000)
                {
                    Log.Info("You were trying to open large number of files ({0}). A Microsoft bug prevents this. Solutions:\n" +
                        "a) Open the files in smaller bulks.\nb) Make a single ZIP file and open it instead.\nc) Drag&Drop the files to the application area directly from the folder.", fileCount);
                }
            }
        }

        /// <summary>
        /// Load structures from files.
        /// </summary>
        /// <param name="files"></param>
        public async Task Load(IEnumerable<FileInfo> files)
        {
            var cs = ComputationService.Default;

            cs.Start();

            var filesArray = files.ToArray();
            int fileCount = filesArray.Count();
            cs.Progress.UpdateIsIndeterminate(false);
            cs.Progress.UpdateProgress(0, fileCount);
            cs.Progress.UpdateStatus("Reading structures...");

            try
            {
                var sources = await TaskEx.Run(() => filesArray.SelectMany((fi, index) =>
                {
                    cs.Progress.UpdateProgress(index);
                    cs.Progress.UpdateStatus(string.Format("Reading '{0}'...", fi.Name));
                    cs.Progress.ThrowIfCancellationRequested();
                    try
                    {
                        return ReadFile(fi, cs.Progress);
                    }
                    catch (ComputationCancelledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        return Tuple.Create(fi.Name, (string)null).ToSingletonEnumerable();
                    }
                }).ToArray());

                var structures = sources.Where(s => StructureReader.IsStructureFilename(s.Item1)).ToArray();
                if (structures.Length > 0) await LoadMany(structures, cs);

                var props = sources.Where(s => s.Item1.EndsWith(AtomPropertiesEx.DefaultExtension, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (props.Length > 0)
                {
                    await LoadProperties(props, cs);
                    QueryExecutionContext.ResetCache();
                }
            }
            catch (ComputationCancelledException)
            {
                Log.Info("Loading cancelled.");
                cs.End();
            }
            finally
            {
                cs.End();
            }
        }
        
        public static bool IsOpenable(string filename)
        {
            return StructureReader.IsStructureFilename(filename) 
                || filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(AtomPropertiesEx.DefaultExtension, StringComparison.OrdinalIgnoreCase);
        }

        static IEnumerable<Tuple<string, string>> ReadFile(FileInfo fi, ComputationProgress progress)
        {
            if (!fi.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                string source = "";
                using (var r = fi.OpenText()) source = r.ReadToEnd();
                yield return Tuple.Create(fi.Name, source);
            }
            else if (fi.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using (var ms = new MemoryStream((int)fi.Length))
                {
                    using (var stream = fi.OpenRead())
                    {
                        stream.CopyTo(ms);
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    foreach (var entry in ZipUtils.EnumerateZipContent(ms, s => !s.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && IsOpenable(s), progress)) yield return entry;
                }
            }
        }

        async Task LoadMany(IList<Tuple<string, string>> sources, ComputationService cs)
        {
            try
            {
                int errors = 0;
                int index = 0;
                int warningCount = 0;
                List<TStructure> news = new List<TStructure>();

                cs.Progress.UpdateIsIndeterminate(false);
                cs.Progress.UpdateProgress(0, sources.Count);
                cs.Progress.UpdateStatus("Parsing structures...");

                await TaskEx.Run(() =>
                {
                    var newIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var f in sources)
                    {
                        cs.Progress.ThrowIfCancellationRequested();
                        index++;
                        cs.Progress.UpdateProgress(index);

                        if (string.IsNullOrEmpty(f.Item2))
                        {
                            if (errors < 50) Log.Error("Reading File", f.Item1);
                            errors++;
                            continue;
                        }

                        try
                        {
                            var s = StructureWrapBase<TStructure>.Create(f.Item1, f.Item2, this);
                            if (StructureMap.ContainsKey(s.Structure.Id) || newIds.Contains(s.Structure.Id))
                            {
                                if (errors < 50) Log.Warning("{0}: {1}", s.Structure.Id, "A structure with this ID is already loaded.");
                                errors++;
                                continue;
                            }
                            news.Add(s);
                            newIds.Add(s.Structure.Id);
                        }
                        catch (Exception e)
                        {
                            if (errors < 50) Log.Error("Load", "{0}: {1}", f.Item1, e.Message);
                            errors++;
                        }
                    }
                });

                if (errors >= 50)
                {
                    Log.Error("Load", "...and {0} more errors.", errors - 50);
                }

                cs.Progress.Update(statusText: "Loading...", canCancel: false, isIndeterminate: true);
                await TaskEx.Delay(TimeSpan.FromSeconds(0.05));

                Structures = new ObservableCollection<TStructure>(Structures.Concat(news));
                StructuresView = new PagedCollectionView(Structures);
                UpdateStructuresView();

                foreach (var s in news)
                {
                    warningCount += s.Warnings.Count;
                    StructureMap.Add(s.Structure.Id, s);
                    QueryExecutionContext.Add(s.Structure);
                }

                await Descriptors.Compute(news, cs.Progress);

                cs.Progress.Update(statusText: "Loading...", canCancel: false, isIndeterminate: true);
                await TaskEx.Delay(TimeSpan.FromSeconds(0.05));

                OnLoad(news);

                var timing = cs.Elapsed;

                if (warningCount > 0)
                {
                    Log.Warning("{0} warning(s) while loading structures. To view them, use 'Session.ShowWarnings()' in scripting window.", warningCount);
                }

                if (errors == 0 && warningCount == 0) Log.Message("Loaded {0} file(s) in {2}.", news.Count, errors, ComputationService.GetElapasedTimeString(timing));
                else if (errors > 0 && warningCount > 0) Log.Message("Loaded {0} file(s), {1} error(s), {2} warning(s) in {3}.", news.Count, errors, warningCount, ComputationService.GetElapasedTimeString(timing));
                else if (errors > 0) Log.Message("Loaded {0} file(s), {1} error(s) in {3}.", news.Count, errors, warningCount, ComputationService.GetElapasedTimeString(timing));
                else Log.Message("Loaded {0} file(s), {2} warning(s) in {3}.", news.Count, errors, warningCount, ComputationService.GetElapasedTimeString(timing));
            }
            catch (ComputationCancelledException)
            {
                Log.Message("Loading cancelled.");
            }
        }

        async Task LoadProperties(IList<Tuple<string, string>> sources, ComputationService cs)
        {
            try
            {
                int errors = 0;
                int index = 0;
                int counter = 0;

                cs.Progress.UpdateIsIndeterminate(false);
                cs.Progress.UpdateProgress(0, sources.Count);
                cs.Progress.UpdateStatus("Loading properties...");

                await TaskEx.Run(() =>
                {
                    foreach (var f in sources)
                    {
                        cs.Progress.ThrowIfCancellationRequested();
                        index++;
                        cs.Progress.UpdateProgress(index);

                        if (string.IsNullOrEmpty(f.Item2))
                        {
                            if (errors < 50) Log.Error("Reading File", f.Item1);
                            errors++;
                            continue;
                        }

                        try
                        {
                            var props = AtomPropertiesEx.Read(new StringReader(f.Item2));

                            if (StructureMap.ContainsKey(props.ParentId))
                            {
                                var s = StructureMap[props.ParentId];
                                s.AttachProperties(props);
                                counter++;
                            }
                            else
                            {
                                if (errors < 50) Log.Message("{0}: Could not find structure with id '{1}'.", f.Item1, props.ParentId);
                                errors++;
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            if (errors < 50) Log.Message("{0}: {1}", f.Item1, e.Message);
                            errors++;
                        }
                    }
                });

                if (errors >= 50)
                {
                    Log.Message("...and {0} more errors.", errors - 50);
                }
                
                var timing = cs.Elapsed;
                Log.Message("Loaded {0} properties(s), {1} error(s).", counter, errors);
            }
            catch (ComputationCancelledException)
            {
                Log.Message("Loading cancelled.");
            }
        }

        /// <summary>
        /// Called after the new structures are loaded.
        /// </summary>
        /// <param name="newStructures"></param>
        protected abstract void OnLoad(List<TStructure> newStructures);

        /// <summary>
        /// Removes a single structure.
        /// </summary>
        /// <param name="structure"></param>
        public void Remove(TStructure structure)
        {
            Structures.Remove(structure);
            StructureMap.Remove(structure.Structure.Id);
            QueryExecutionContext.Remove(structure.Structure);
            OnRemove(structure);
            structure.Dispose();
        }

        /// <summary>
        /// Remove many structures at once.
        /// This recreates the view.
        /// </summary>
        /// <param name="structures"></param>
        public void RemoveMany(IList<TStructure> structures)
        {
            var set = structures.ToHashSet();
            var ns = new ObservableCollection<TStructure>(Structures.Where(s => !set.Contains(s)));

            foreach (var s in structures)
            {
                StructureMap.Remove(s.Structure.Id);
                QueryExecutionContext.Remove(s.Structure);
            }

            OnRemoveMany(structures);

            foreach (var s in structures)
            {
                s.Dispose();
            }

            Structures = ns;
            StructuresView = new PagedCollectionView(ns);
            UpdateStructuresView();
        }

        /// <summary>
        /// Clear the session.
        /// </summary>
        public virtual void Clear()
        {
            var snapshot = Structures.ToArray();
            OnClear(snapshot);
            StructureMap.Clear();
            Structures = new ObservableCollection<TStructure>();
            StructuresView = new PagedCollectionView(Structures);
            foreach (var s in snapshot) s.Dispose();
        }

        /// <summary>
        /// Called on remove.
        /// </summary>
        /// <param name="structure"></param>
        protected abstract void OnRemove(TStructure structure);

        /// <summary>
        /// Called on remove many.
        /// </summary>
        /// <param name="structures"></param>
        protected abstract void OnRemoveMany(IList<TStructure> structures);

        /// <summary>
        /// Called on clear to.
        /// </summary>
        protected abstract void OnClear(IList<TStructure> structures);

        /// <summary>
        /// Save the state to an XML element.
        /// </summary>
        /// <returns></returns>
        protected abstract XElement SaveStateToXml();

        /// <summary>
        /// Load the state from an XML element.
        /// </summary>
        /// <param name="root"></param>
        protected abstract void LoadStateFromXml(XElement root);

        /// <summary>
        /// Saves the session. Uses save file dialog.
        /// </summary>
        public async void SaveSession()
        {
            var sfd = new SaveFileDialog
            {
                Filter = string.Format("{0} Workspace (*{1})|*{1}", Name, WorkspaceExtension)
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Saving...");
                try
                {
                    using (var stream = sfd.OpenFile())
                    using (var zip = new ZipOutputStream(stream))
                    {
                        await TaskEx.Run(() => WriteToZipStream(zip));
                    }
                    Log.Message("Session saved.");
                }
                catch (Exception e)
                {
                    Log.Error("Saving Session", e.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }

        void WriteToZipStream(ZipOutputStream zip)
        {
            ZipUtils.WriteStructuresToZipStream(zip, "structures", Structures, s => ZipUtils.DecompressString(s.CompressedSource));
            var writer = new StreamWriter(zip);
            
            zip.PutNextEntry(new ZipEntry(WorkspaceFilename));
            writer.Write(SaveStateToXml().ToString());
            writer.Flush();
            zip.CloseEntry();

            zip.PutNextEntry(new ZipEntry(QueryHistoryFilename));
            writer.Write(QueryService.Default.ExportHistory().ToString());
            writer.Flush();
            zip.CloseEntry();
        }
        
        /// <summary>
        /// Load a session.
        /// </summary>
        public void LoadSession()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = string.Format("{0} Workspace (*{1})|*{1}", Name, WorkspaceExtension)
                };

                if (ofd.ShowDialog() == true)
                {
                    LoadWorkspace(ofd.File);
                }
            }
            catch (Exception e)
            {
                Log.Error("Loading Workspace", e.Message);
            }
        }

        public void LoadWorkspace(FileInfo fi)
        {
            LoadWorkspace(() => fi.OpenRead(), (int)fi.Length);           
        }

        public async void LoadWorkspace(Func<Stream> streamProvider, int streamLength)
        {
            var cs = ComputationService.Default;
            var progress = cs.Start();

            try
            {
                using (var ms = new MemoryStream(streamLength))
                {
                    progress.UpdateStatus("Reading workspace...");
                    progress.UpdateIsIndeterminate(true);
                    progress.UpdateCanCancel(false);
                    using (var stream = streamProvider())
                    {
                        await stream.CopyToAsync(ms);
                    }

                    progress.UpdateCanCancel(true);

                    ms.Seek(0, SeekOrigin.Begin);

                    List<Tuple<string, string>> structures = new List<Tuple<string, string>>();
                    string workspace = null;
                    string qhistory = null;

                    await TaskEx.Run(() =>
                    {
                        foreach (var entry in ZipUtils.EnumerateZipContent(ms, s => StructureReader.IsStructureFilename(s) || s == WorkspaceFilename, progress))
                        {
                            if (entry.Item1 == WorkspaceFilename) workspace = entry.Item2;
                            else if (entry.Item1 == QueryHistoryFilename) qhistory = entry.Item2;
                            else structures.Add(entry);
                        }
                    });

                    Clear();
                    await LoadMany(structures, cs);

                    var ws = await TaskEx.Run(() => XElement.Parse(workspace));
                    LoadStateFromXml(ws);

                    if (qhistory != null) QueryService.Default.LoadHistory(XElement.Parse(qhistory));

                    Log.Message("Session loaded.");
                }
            }
            catch (Exception e)
            {
                Log.Error("Loading Workspace", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        public async void ExportToZip()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip"
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Exporting...");
                try
                {
                    using (var stream = sfd.OpenFile())
                    using (var zip = new ZipOutputStream(stream))
                    {
                        await TaskEx.Run(() => ExportToZip(zip, progress));
                    }
                    Log.Message("Export successful.");
                }
                catch (Exception e)
                {
                    Log.Error("Export", e.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }

        /// <summary>
        /// Export the session to a zip stream...
        /// </summary>
        /// <param name="zip"></param>
        /// <param name="progress"></param>
        protected abstract void ExportToZip(ZipOutputStream zip, ComputationProgress progress);

        /// <summary>
        /// Workspace extension with dot.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="workspaceExtension"></param>
        protected SessionBase(string name, string workspaceExtension)
        {
            ////// JIT motive query
            ////try
            ////{
            ////    var q = Query.Parse("1+1");
            ////    q.ExecuteDynamic(null);
            ////}
            ////catch { }

            this.Name = name;
            this.WorkspaceExtension = workspaceExtension;

            _structures = new ObservableCollection<TStructure>();
            _structuresView = new PagedCollectionView(_structures);

            this.QueryExecutionContext = ExecutionContext.Create(new IStructure[0]);
            this.QueriesScripting = new Utils.QueriesScripting(this);
            this.UtilsScripting = new Utils.UtilsScripting(this);
            this.Descriptors = StructureDescriptorsModel<TStructure>.Create(this);

            ScriptService.Default.InitSession(this);
        }
    }
}
