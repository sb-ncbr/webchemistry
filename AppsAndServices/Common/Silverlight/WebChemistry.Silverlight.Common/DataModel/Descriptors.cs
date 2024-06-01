using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using GalaSoft.MvvmLight.Command;
using WebChemistry.Silverlight.Common.Services;
using System.Threading.Tasks;
using WebChemistry.Framework.TypeSystem;
using System.IO;

namespace WebChemistry.Silverlight.Common.DataModel
{
    /// <summary>
    /// Descriptor wrapper.
    /// </summary>
    public class DescriptorWrapper : InteractiveObject
    {
        /// <summary>
        /// Input expression.
        /// </summary>
        public string InputExpression { get; private set; }

        /// <summary>
        /// The actual query.
        /// </summary>
        public Query Query { get; private set; }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type.
        /// </summary>
        public TypeExpression Type { get; private set; }
        
        /// <summary>
        /// Create the thing.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expression"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static DescriptorWrapper Create(string name, string expression, Query query, TypeExpression type)
        {
            return new DescriptorWrapper 
            {
                Name = name.Trim(),
                Query = query,
                Type = type,
                InputExpression = expression.Trim(),
                IsSelected = true
            };
        }

        private DescriptorWrapper()
        {

        }

        public override string ToString()
        {
            return Name;
        }

        internal object Compute<T>(StructureWrapBase<T> structure, ExecutionContext context)
            where T : StructureWrapBase<T>, new()
        {
            context.CurrentContext = structure.Structure.MotiveContext();
            context.CurrentMotive = context.CurrentContext.StructureMotive;
            return Query.ExecuteObject(context);
        }

        internal void SetValue<T>(StructureWrapBase<T> structure, object value)
            where T : StructureWrapBase<T>, new()
        {
            structure.Descriptors[Name] = value;
        }

        internal void Remove<T>(StructureWrapBase<T> structure)
            where T : StructureWrapBase<T>, new()
        {
            structure.Descriptors.RemoveDescriptor(Name);
        }
    }

    /// <summary>
    /// The descriptors model.
    /// </summary>
    public class StructureDescriptorsModel<T> : ObservableObject
        where T : StructureWrapBase<T>, new()
    {
        SessionBase<T> session;

        private ICommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                _addCommand = _addCommand ?? new RelayCommand(() => Add());
                return _addCommand;
            }
        }

        private ICommand _removeCurrentCommand;
        public ICommand RemoveCurrentCommand
        {
            get
            {
                _removeCurrentCommand = _removeCurrentCommand ?? new RelayCommand(() => RemoveCurrent());
                return _removeCurrentCommand;
            }
        }
        
        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                _exportCommand = _exportCommand ?? new RelayCommand<string>(what => Export(what));
                return _exportCommand;
            }
        }

        private ICommand _selectCommand;
        public ICommand SelectCommand
        {
            get
            {
                _selectCommand = _selectCommand ?? new RelayCommand<string>(what => Select(what));
                return _selectCommand;
            }
        }

        
        /// <summary>
        /// Descriptors.
        /// </summary>
        public ObservableCollection<DescriptorWrapper> Descriptors { get; private set; }

        /// <summary>
        /// Sorted view.
        /// </summary>
        public PagedCollectionView DescriptorsView { get; private set; }

        private DescriptorWrapper _currentDescriptor;
        /// <summary>
        /// Current descriptor.
        /// </summary>
        public DescriptorWrapper CurrentDescriptor
        {
            get
            {
                return _currentDescriptor;
            }

            set
            {
                if (_currentDescriptor == value) return;

                _currentDescriptor = value;
                NotifyPropertyChanged("CurrentDescriptor");

                if (value == null) session.Structures.ForEach(s => s.ResetCurrentDescriptor());
                else session.Structures.ForEach(s => s.SetCurrentDescriptor(value.Name));
                session.CurrentDescriptorUpdated();
            }
        }

        private string _currentExpression;
        /// <summary>
        /// Current expression.
        /// </summary>
        public string CurrentExpression
        {
            get
            {
                return _currentExpression;
            }

            set
            {
                if (_currentExpression == value) return;

                _currentExpression = value;
                NotifyPropertyChanged("CurrentExpression");
            }
        }

        private string _currentName;
        /// <summary>
        /// Current descriptor name.
        /// </summary>
        public string CurrentName
        {
            get
            {
                return _currentName;
            }

            set
            {
                if (_currentName == value) return;

                _currentName = value;
                NotifyPropertyChanged("CurrentName");
            }
        }

        /// <summary>
        /// Create new.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static StructureDescriptorsModel<T> Create(SessionBase<T> session)
        {
            var ret = new StructureDescriptorsModel<T> 
            {
                session = session,
                Descriptors = new ObservableCollection<DescriptorWrapper>()
            };

            ret.DescriptorsView = new PagedCollectionView(ret.Descriptors);
            ret.DescriptorsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            return ret;
        }

        static void Compute(SessionBase<T> session, IList<DescriptorWrapper> descriptorWrappers, IEnumerable<StructureWrapBase<T>> structures, ComputationProgress progress)
        {
            var sa = structures.ToArray();

            object[][] descriptors = new object[descriptorWrappers.Count][];

            int index = 0;
            foreach (var d in descriptorWrappers)
            {
                var ld = new object[sa.Length];
                descriptors[index++] = ld;
                progress.Update(statusText: string.Format("Computing descriptor '{0}'...", d.Name), currentProgress: 0, maxProgress: sa.Length, isIndeterminate: false, canCancel: true);
                for (int i = 0; i < sa.Length; i++)
                {
                    progress.ThrowIfCancellationRequested();
                    ld[i] = d.Compute(sa[i], session.QueryExecutionContext);
                    progress.UpdateProgress(i);
                }
            }
            
            progress.Update(statusText: string.Format("Updaring descriptors..."), isIndeterminate: true, canCancel: false);
            for (int i = 0; i < descriptorWrappers.Count; i++)
            {
                var d = descriptorWrappers[i];
                var desc = descriptors[i];
                for (int j = 0; j < sa.Length; j++)
                {
                    d.SetValue(sa[j], desc[j]);
                }
            }
        }

        /// <summary>
        /// Adds a descriptor. Does not compute the values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        DescriptorWrapper AddInternal(string name, string expression)
        {
            name = name.Trim();

            if (string.IsNullOrEmpty(name))
            {
                LogService.Default.Error("Descriptors", "Cannot add descriptor: The name cannot be empty.");
                return null;
            }

            ////if (Descriptors.Any(d => d.Name.EqualOrdinalIgnoreCase(name)))
            ////{
            ////    LogService.Default.Error("Descriptors", "A descriptor called '{0}' already exists.", name);
            ////    return null;
            ////}

            try
            {
                var mq = ScriptService.Default.GetMetaQuery(expression, BasicTypes.Value);
                var comp = mq.Compile();
                var ret = DescriptorWrapper.Create(name, expression, comp, mq.Type);                
                return ret;
            }
            catch (Exception e)
            {
                LogService.Default.Error("Descriptors", "Cannot add descriptor '{0}': {1}", name, e.Message);
                return null;
            }
        }

        void AddWrapper(DescriptorWrapper w)
        {
            if (!Descriptors.Any(d => d.Name.EqualOrdinalIgnoreCase(w.Name))) Descriptors.Add(w);
        }

        async void Add()
        {
            var desc = AddInternal(CurrentName, CurrentExpression);
            if (desc == null) return;

            try
            {
                var progress = ComputationService.Default.Start();
                await TaskEx.Run(() => Compute(session, desc.ToSingletonArray(), session.Structures, progress));
                CurrentName = "";
                CurrentExpression = "";
                AddWrapper(desc);
                QueryService.Default.AddQueryHistory(desc.InputExpression);
                LogService.Default.Message("Descriptor '{0}' added.", desc.Name);
                if (CurrentDescriptor == null) CurrentDescriptor = desc;
            }
            catch (ComputationCancelledException)
            {
                LogService.Default.Aborted();
            }
            catch (Exception e)
            {
                LogService.Default.Error("Descriptors", e.Message);
            }
            finally
            {
                ComputationService.Default.End();
            }
        }

        public async Task Add(string name, string expression)
        {
            var desc = AddInternal(name, expression);
            if (desc == null) return;

            try
            {
                var progress = ComputationService.Default.Start();
                await TaskEx.Run(() => Compute(session, desc.ToSingletonArray(), session.Structures, progress));
                AddWrapper(desc);
                QueryService.Default.AddQueryHistory(desc.InputExpression);
                LogService.Default.Message("Descriptor '{0}' added.", desc.Name);
                if (CurrentDescriptor == null) CurrentDescriptor = desc;
            }
            catch (ComputationCancelledException)
            {
                LogService.Default.Aborted();
            }
            catch (Exception e)
            {
                LogService.Default.Error("Descriptors", e.Message);
            }
            finally
            {
                ComputationService.Default.End();
            }
        }

        public void AddSynchronously(string name, string expression)
        {
            var desc = AddInternal(name, expression);
            if (desc == null) return;

            try
            {
                Compute(session, desc.ToSingletonArray(), session.Structures, ComputationProgress.DummyInstance);
                AddWrapper(desc);             
                if (CurrentDescriptor == null) CurrentDescriptor = desc;
            }
            catch (Exception e)
            {
                LogService.Default.Error("Descriptors", e.Message);
            }
        }

        void RemoveCurrent()
        {
            if (CurrentDescriptor == null) return;

            Remove(CurrentDescriptor);
            if (Descriptors.Count > 0)
            {
                CurrentDescriptor = Descriptors[0];
            }
            else
            {
                CurrentDescriptor = null;
            }
        }

        /// <summary>
        /// Remove descriptor.
        /// </summary>
        /// <param name="descriptor"></param>
        public void Remove(DescriptorWrapper descriptor)
        {
            session.Structures.ForEach(s => descriptor.Remove(s));
            Descriptors.Remove(descriptor);
        }

        /// <summary>
        /// Compute descriptor.
        /// </summary>
        /// <param name="structures"></param>
        /// <param name="progress"></param>
        public async Task Compute(IEnumerable<StructureWrapBase<T>> structures, ComputationProgress progress)
        {
            await TaskEx.Run(() => Compute(session, Descriptors, structures, progress));
            if (CurrentDescriptor != null)
            {
                structures.ForEach(s => s.SetCurrentDescriptor(CurrentDescriptor.Name));
                session.CurrentDescriptorUpdated();
            }
        }

        public ListExporter<T> AddColumns(ListExporter<T> exporter)
        {
            foreach (DescriptorWrapper d in DescriptorsView)
            {
                if (!d.IsSelected) continue;

                var name = d.Name;
                bool isNumber = TypeExpression.Unify(BasicTypes.Number, d.Type).Success;

                exporter = exporter.AddExportableColumn(s =>
                {
                    var x = s.Descriptors[name];
                    if (x == null) return "-";
                    if (x is double) return ((double)x).ToStringInvariant();
                    return x.ToString();
                }, isNumber ? ColumnType.Number : ColumnType.String, name);
            }

            return exporter;
        }

        public ListExporter<TItem> AddColumns<TItem>(ListExporter<TItem> exporter, Func<TItem, T> selector)
            where TItem : class
        {
            foreach (DescriptorWrapper d in DescriptorsView)
            {
                var name = d.Name;
                bool isNumber = TypeExpression.Unify(BasicTypes.Number, d.Type).Success;

                exporter = exporter.AddExportableColumn(s =>
                {
                    var x = selector(s).Descriptors[name];
                    if (x == null) return "-";
                    if (x is double) return ((double)x).ToStringInvariant();
                    return x.ToString();
                }, isNumber ? ColumnType.Number : ColumnType.String, name);
            }

            return exporter;
        }
        
        string ToCsv(string separator)
        {
            List<T> toExport = new List<T>();

            foreach (T s in session.StructuresView)
            {
                if (s.IsSelected) toExport.Add(s);
            }

            var exporter = toExport.GetExporter(separator: separator)
                .AddExportableColumn(s => s.Structure.Id, ColumnType.String, "Id")
                .AddExportableColumn(s => s.Structure.Atoms.Count, ColumnType.Number, "AtomCount")
                .AddExportableColumn(s => s.Structure.PdbResidues().CountedResidueString, ColumnType.String, "Residues");

            exporter = AddColumns(exporter);

            return exporter.ToCsvString();
        }


        void Select(string what)
        {
            switch (what.ToLower())
            {
                case "all": Descriptors.ForEach(d => d.IsSelected = true); break;
                case "none": Descriptors.ForEach(d => d.IsSelected = false); break;
            }

            return;
        }

        void Export(string what)
        {
            try
            {
                switch (what.ToLowerInvariant())
                {
                    case "copy":
                        {
                            Clipboard.SetText(ToCsv("\t"));
                            LogService.Default.Message("Descriptors copied to clipboard.");
                            break;
                        }
                    case "save":
                        {
                            var sfd = new SaveFileDialog
                            {
                                Filter = "CSV files (*.csv)|*.csv"
                            };

                            if (sfd.ShowDialog() == true)
                            {
                                using (var stream = sfd.OpenFile())
                                using (var w = new StreamWriter(stream))
                                {
                                    w.Write(ToCsv(","));
                                }
                                LogService.Default.Message("Descriptors exported.");
                            }
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                LogService.Default.Error("Export", e.Message);
            }
        }

        private StructureDescriptorsModel()
        {

        }
    }
}
