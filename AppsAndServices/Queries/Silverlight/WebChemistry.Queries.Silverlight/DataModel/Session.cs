using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Silverlight.Visuals;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Queries.Silverlight.DataModel
{
    public class Session : SessionBase<StructureWrap>
    {
        PagedCollectionView _motivesView;
        public PagedCollectionView MotivesView
        {
            get { return _motivesView; }
            set
            {
                if (_motivesView == value) return;
                _motivesView = value;
                NotifyPropertyChanged("MotivesView");
            }
        }

        string _motiveQueryString = "";
        public string QueriesString
        {
            get { return _motiveQueryString; }
            set
            {
                if (_motiveQueryString == value) return;
                _motiveQueryString = value;
                NotifyPropertyChanged("QueriesString");
            }
        }

        public override void UpdateStructuresView()
        {
            using (StructuresView.DeferRefresh())
            {
                StructuresView.SortDescriptions.Clear();
                StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
            }
        }


        string[] motiveListGroupingTypes = new string[] { "Group by Parent", "Group by Residues", "Group by Atom Count", "No Groups" };
        public string[] MotiveListGroupingTypes { get { return motiveListGroupingTypes; } }

        void UpdateMotivesGrouping()
        {
            if (MotivesView == null) return;

            using (MotivesView.DeferRefresh())
            {
                this.MotivesView.SortDescriptions.Clear();
                this.MotivesView.SortDescriptions.Add(new SortDescription("Parent.Id", ListSortDirection.Ascending));
                this.MotivesView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                this.MotivesView.GroupDescriptions.Clear();
                switch (GroupingTypeIndex)
                {
                    case 0:
                        this.MotivesView.GroupDescriptions.Add(new PropertyGroupDescription("Parent.Id") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 1:
                        this.MotivesView.GroupDescriptions.Add(new PropertyGroupDescription("ResidueString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 2:
                        this.MotivesView.GroupDescriptions.Add(new PropertyGroupDescription("AtomCountString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    default:
                        break;
                }
            }
        }

        private int _groupingTypeIndex = 0;
        public int GroupingTypeIndex
        {
            get
            {
                return _groupingTypeIndex;
            }

            set
            {
                if (_groupingTypeIndex == value) return;

                _groupingTypeIndex = value;
                UpdateMotivesGrouping();
                NotifyPropertyChanged("GroupingTypeIndex");
            }
        }


        InteractiveObject _currentlySelected;
        public InteractiveObject CurrentlySelected
        {
            get { return _currentlySelected; }
            set
            {
                if (_currentlySelected == value) return;
                _currentlySelected = value;
                NotifyPropertyChanged("CurrentlySelected");
            }
        }

        List<MotiveWrap> Motives = new List<MotiveWrap>();

        MotiveVisual3DWrap visual = new MotiveVisual3DWrap();
        public MotiveVisual3DWrap Visual { get { return visual; } }
        
        bool avoidFeedback = false;
        void SelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (avoidFeedback) return;

            if (e.PropertyName.Equals("IsSelected"))
            {
                if (CurrentlySelected != null && !object.ReferenceEquals(CurrentlySelected, sender))
                {
                    avoidFeedback = true;
                    CurrentlySelected.IsSelected = false;
                    avoidFeedback = false;
                }
                
                if (Visual.Visual != null && !Visual.Visual.IsSelected) Visual.Clear();

                var obj = sender as InteractiveObject;

                if (obj.IsSelected)
                {
                    CurrentlySelected = obj;
                    if (obj is IVisual) visual.SetVisual(obj as IVisual);
                }
                else
                {
                    visual.Clear();
                    CurrentlySelected = null;
                }
            }
        }
        
        protected override void OnRemove(StructureWrap structure)
        {
            if (structure.IsSelected) CurrentlySelected.IsSelected = false;
            structure.PropertyChanged -= SelectionChanged;

            if (structure.MotiveCount > 0)
            {
                List<MotiveWrap> newMotives = new List<MotiveWrap>();
                foreach (var m in Motives)
                {
                    if (m.Parent != structure.Structure)
                    {
                        newMotives.Add(m);
                        continue;
                    }

                    if (m.IsSelected) m.IsSelected = false;
                    m.PropertyChanged -= SelectionChanged;
                    m.ClearVisual();
                }

                Motives = newMotives;
                MotivesView = new PagedCollectionView(Motives);
                UpdateMotivesGrouping();
            }
        }

        protected override void OnRemoveMany(IList<StructureWrap> structures)
        {
            foreach (var s in structures) OnRemove(s);
        }

        protected override void OnClear(IList<StructureWrap> structures)
        {
            if (CurrentlySelected != null) CurrentlySelected.IsSelected = false;

            MotivesView = null;

            structures.ForEach(s => { s.ClearVisual(); s.PropertyChanged -= SelectionChanged; });
            Motives.ForEach(s => { s.ClearVisual(); s.PropertyChanged -= SelectionChanged; });

            Motives = new List<MotiveWrap>();
        }

        protected override void OnLoad(List<StructureWrap> newStructures)
        {
            foreach (var s in newStructures) s.PropertyChanged += SelectionChanged;
        }
                
        public async void DoQuery()
        {
            Query query;
            try
            {
                var mq = ScriptService.Default.GetMetaQuery(QueriesString, BasicTypes.PatternSeq);
                query = mq.Compile();
            }
            catch (Exception e)
            {
                Log.Error("Query", e.Message);
                return;
            }

            var hq = QueriesString.Trim();
            QueryService.Default.AddQueryHistory(hq);

            var cs = ComputationService.Default;
            var progress = cs.Start();

            try
            {
                progress.UpdateStatus("Working...");
                progress.UpdateProgress(0, Structures.Count);

                int totalFound = 0;

                List<MotiveWrap> result = new List<MotiveWrap>();
                Dictionary<StructureWrap, int> motiveCounts = new Dictionary<StructureWrap, int>();

                await TaskEx.Run(() =>
                    {
                        int si = 0;
                        foreach (var s in Structures)
                        {
                            progress.ThrowIfCancellationRequested();
                            progress.UpdateProgress(si++);
                            var matches = query.Matches(s.Structure);

                            for (int i = 0; i < matches.Count; i++)
                            {
                                var m = MotiveWrap.Create(s.Structure, matches[i].ToStructure((i+1).ToString(), true, true), i + 1);
                                result.Add(m);
                            }

                            motiveCounts[s] = matches.Count;
                            totalFound += matches.Count;
                        }
                    });

                Log.Message("{0} pattern(s) found in {1}.", totalFound, ComputationService.GetElapasedTimeString(cs.Elapsed));

                if (CurrentlySelected != null)
                {
                    CurrentlySelected.IsSelected = false;
                    CurrentlySelected = null;
                }
                Structures.ForEach(s => { s.MotiveCount = motiveCounts[s]; s.Structure.SelectAllAtoms(false); s.ClearVisual(); });
                foreach (var m in result)
                {
                    m.PropertyChanged += SelectionChanged;
                    m.Motive.ToCentroidCoordinates();
                    m.Motive.SelectAllAtoms(true);
                    m.Motive.Atoms.ForEach(a => m.Parent.Atoms.GetById(a.Id).IsSelected = true);
                }
                foreach (var m in Motives)
                {
                    m.PropertyChanged -= SelectionChanged;
                    m.ClearVisual();
                }
                Motives = result;
                MotivesView = new PagedCollectionView(result);
                UpdateMotivesGrouping();
            }
            catch (ComputationCancelledException)
            {
                Log.Aborted();
            }
            catch (Exception e)
            {
                Log.Error("Query", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        protected override void ExportToZip(ZipOutputStream zip, ComputationProgress progress)
        {
            var writer = new StreamWriter(zip);

            var om = Motives.OrderBy(m => m.Name).ToArray();
            var exporter = MotiveWrap.GetExporter(om);

            WriteMotivesToZipStream(zip, writer, "motives", om);
                
            zip.PutNextEntry(new ZipEntry("index.csv"));
            exporter.WriteCsvString(writer);
            writer.Flush();
            zip.CloseEntry();

            zip.PutNextEntry(new ZipEntry("index.xml"));
            writer.Write(exporter.ToXml().ToString());
            writer.Flush();
            zip.CloseEntry();
        }

        static void WriteMotivesToZipStream(ZipOutputStream stream, StreamWriter writer, string folder, IEnumerable<MotiveWrap> structures)
        {
            foreach (var s in structures)
            {
                var entry = new ZipEntry(ZipEntry.CleanName(folder + "\\" + s.Name + ".pdb"));
                stream.PutNextEntry(entry);
                s.Motive.WritePdb(writer);
                writer.Flush();
                stream.CloseEntry();
            }
        }

        protected override XElement SaveStateToXml()
        {
            return new XElement("Workspace",
                new XAttribute("Query", QueriesString));
        }

        protected override void LoadStateFromXml(XElement root)
        {
            QueriesString = root.Attribute("Query").Value;
        }

        public Session()
            : base("MotiveExplorer", ".mws")
        {
        }
    }
}
