using System;
using System.Linq;
using System.IO;
using WebChemistry.Framework.Core;
using System.Text;
using WebChemistry.Queries.Core;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Collections.ObjectModel;

namespace WebChemistry.Silverlight.Common.DataModel
{
    public abstract class StructureWrapBase<T> : InteractiveObject, IDisposable where T : StructureWrapBase<T>, new()
    {

        /// <summary>
        /// Get current structure selection string.
        /// </summary>
        /// <returns></returns>
        public Tuple<int, string> GetSelectionString()
        {
            int count = 0;
            var ret = string.Join(" ", Structure.Atoms
                .Where(a => a.IsSelected)
                .GroupBy(a => a.ElementSymbol)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var c = g.Count();
                    count += c;
                    return c.ToString() + g.Key.ToString();
                }));

            if (count == 0) return Tuple.Create(0, "Empty");
            return Tuple.Create(count, ret);
        }
        
        StructureDescriptors descriptors;

        /// <summary>
        /// The descriptors.
        /// </summary>
        public StructureDescriptors Descriptors { get { return descriptors; } }

        /// <summary>
        /// CurrentDescriptor object.
        /// </summary>
        public object CurrentDescriptorValue { get; private set; }

        private string _formattedCurrentDescriptor;
        /// <summary>
        /// Formatted current descriptor.
        /// </summary>
        public string FormattedCurrentDescriptor
        {
            get
            {
                return _formattedCurrentDescriptor;
            }

            set
            {
                if (_formattedCurrentDescriptor == value) return;

                _formattedCurrentDescriptor = value;
                NotifyPropertyChanged("FormattedCurrentDescriptor");
            }
        }

        /// <summary>
        /// The session this structure belongs to.
        /// </summary>
        public SessionBase<T> Session { get; private set; }

        /// <summary>
        /// Original filename.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Compressed using ZipUtils.CompressString.
        /// Retrieve using ZipUtils.DecompressString.
        /// </summary>
        public byte[] CompressedSource { get; private set; }

        /// <summary>
        /// The actual structure.
        /// </summary>
        public IStructure Structure { get; private set; }

        /// <summary>
        /// Warnings for the structure.
        /// </summary>
        public ReadOnlyCollection<StructureReaderWarning> Warnings { get; private set; }

        /// <summary>
        /// Structure type.
        /// </summary>
        public StructureType StructureType { get; private set; }

        /// <summary>
        /// Atom selection.
        /// </summary>
        public AtomSelection<T> Selection { get; private set; }

        /// <summary>
        /// Updates the session.
        /// </summary>
        /// <param name="session"></param>
        public void UpdateSession(SessionBase<T> session)
        {
            this.Session = session;
        }

        /// <summary>
        /// Create the wrap.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Create(string filename, string source, SessionBase<T> session)
        {
            StructureType type = StructureReader.GetStructureType(filename); 
            var s = StructureReader.ReadString(filename, source);
            
            var ret = new T
            {
                Filename = filename,
                StructureType = type,
                Structure = s.Structure,
                descriptors = s.Structure.Descriptors(),
                Warnings = s.Warnings,
                Session = session,
                CompressedSource = ZipUtils.CompressString(source)
            };

            ret.Selection = new AtomSelection<T>(ret, session);
            ret.OnCreate();

            s.Structure.ObserveInteractivePropertyChanged(ret, (w, str) => { w.ignoreInteractive = true; w.IsSelected = str.IsSelected; w.ignoreInteractive = true; w.IsHighlighted = str.IsHighlighted; w.ignoreInteractive = false; });

            return ret;
        }

        /// <summary>
        /// Update the current descriptor.
        /// </summary>
        /// <param name="name"></param>
        public void SetCurrentDescriptor(string name)
        {
            CurrentDescriptorValue = Descriptors[name];

            if (CurrentDescriptorValue == null) FormattedCurrentDescriptor = "";
            else if (CurrentDescriptorValue is double) FormattedCurrentDescriptor = ((double)CurrentDescriptorValue).ToStringInvariant("0.00000");
            else FormattedCurrentDescriptor = CurrentDescriptorValue.ToString();
        }

        /// <summary>
        /// Reset the thing.
        /// </summary>
        public void ResetCurrentDescriptor()
        {
            CurrentDescriptorValue = null;
            FormattedCurrentDescriptor = "";
        }

        /// <summary>
        /// Attach atom properties.
        /// </summary>
        /// <param name="props"></param>
        public void AttachProperties(AtomPropertiesBase props)
        {
            Structure.AttachAtomProperties(props);
            OnPropertiesAttached(props);
        }

        /// <summary>
        /// Action on properties attached. This can be executed on any thread!
        /// </summary>
        /// <param name="props"></param>
        protected virtual void OnPropertiesAttached(AtomPropertiesBase props)
        {
        }

        bool ignoreInteractive = false;
        protected override void OnHighlightedChanged()
        {
            if (ignoreInteractive) return;
            ignoreInteractive = true;
            Structure.IsHighlighted = IsHighlighted;
            ignoreInteractive = false;
        }

        protected override void OnSelectedChanged()
        {
            if (ignoreInteractive) return;
            ignoreInteractive = true;
            Structure.IsSelected = IsSelected;
            ignoreInteractive = false;
        }

        /// <summary>
        /// Do not forget to center the structures etc..
        /// </summary>
        protected abstract void OnCreate();

        /// <summary>
        /// SElect atoms.
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="selected"></param>
        public bool SelectAtoms(IEnumerable<IAtom> atoms, bool selected = true)
        {
            if (SelectAtomsInternal(atoms, selected))
            {
                Selection.Update();
                Selection.RaiseSelectionChanged();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// implementation of atom selection
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="selected"></param>
        protected abstract bool SelectAtomsInternal(IEnumerable<IAtom> atoms, bool selected);

        /// <summary>
        /// Get rid of it ...
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public abstract void Dispose();
    }
}
