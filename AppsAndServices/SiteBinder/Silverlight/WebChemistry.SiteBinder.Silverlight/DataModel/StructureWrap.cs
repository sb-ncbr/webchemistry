using System;
using System.Linq;
using WebChemistry.Framework.Core;
using System.IO;
using System.Text;
using WebChemistry.SiteBinder.Core;
using WebChemistry.SiteBinder.Silverlight.Visuals;
using System.Windows.Media;
using System.Collections.Generic;
using WebChemistry.Framework.Geometry;
using System.Windows;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.SiteBinder.Silverlight.DataModel
{
    public class StructureWrap : StructureWrapBase<StructureWrap>
    {
        bool isDirty = true;
        bool ignoreAtomPropertyChanged = false;

        //public Session Session { get; set; }

        private Color _color;
        public Color Color
        {
            get
            {
                return _color;
            }

            set
            {
                if (_color == value) return;

                _color = value;
                if (visual != null)
                {
                    visual.Color = value;
                }
                NotifyPropertyChanged("Color");
            }
        }

        public override string ToString()
        {
            return Structure.Id;
        }

        public int ResidueCount { get; private set; }
        //public string OrderedResidueString { get; private set; }
        public string ResidueString { get; private set; }

        string atomCountString;
        public string AtomCountString { get { return atomCountString; } }

        public int AtomCount { get; private set; }

        public int SelectedCount { get; private set; }
        public string SelectionString { get; private set; }

        public GeometricalCenterInfo CenterInfo { get; private set; }

        public string IdTooltip { get { return Structure.Id + ": " + ResidueString; } }

        //public string SelectionStringWithCount { get; private set; }

        MatchGraph<IAtom> matchGraph;
        public MatchGraph<IAtom> MatchGraph
        {
            get
            {
                if (isDirty || matchGraph == null) matchGraph = WebChemistry.SiteBinder.Core.MatchGraph.Create(Structure, true, (Session as Session).IgnoreHydrogens);
                return matchGraph;
            }
        }

        public bool HasVisual { get { return visual != null; } }

        MotiveVisual3D visual;
        public MotiveVisual3D Visual
        {
            get
            {
                if (visual == null) visual = new MotiveVisual3D(this, Color);
                return visual;
            }
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public Dictionary<string, Dictionary<string, IAtom[]>> ResidueNameGrouping { get; private set; }
        public Dictionary<string, Dictionary<string, IAtom[]>> AtomTypeGrouping { get; private set; }
        public Dictionary<string, Dictionary<string, IAtom[]>> AtomNameGrouping { get; private set; }

        Dictionary<string, IAtom[]> byAtomType, byAtomName, byResidueName;

        void Init()
        {
            orderedAtoms = Structure.Atoms.OrderBy(a => a.ElementSymbol.ToString()).ToArray();
            selectedCounts = orderedAtoms.GroupBy(a => a.ElementSymbol).ToDictionary(g => g.Key, _ => 0);
            atomCountString = Structure.Atoms.Count.ToString();
            AtomCount = Structure.Atoms.Count;
            UpdateSelectionString();

            CenterInfo = Structure.GeometricalCenterAndRadius();

            ResidueNameGrouping = Structure.Atoms
                .GroupBy(a => a.PdbResidueName())
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(a => a.PdbName())
                          .ToDictionary(h => h.Key, h => h.ToArray(), StringComparer.Ordinal),
                    StringComparer.Ordinal);
            byResidueName = Structure.Atoms
                .GroupBy(a => a.PdbResidueName())
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

            AtomTypeGrouping = Structure.Atoms
                .GroupBy(a => a.ElementSymbol.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(a => a.PdbName())
                          .ToDictionary(h => h.Key, h => h.ToArray(), StringComparer.Ordinal),
                    StringComparer.Ordinal);
            byAtomType = Structure.Atoms
                .GroupBy(a => a.ElementSymbol.ToString())
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

            AtomNameGrouping = Structure.Atoms
                .GroupBy(a => a.PdbName())
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(a => a.PdbName())
                          .ToDictionary(h => h.Key, h => h.ToArray(), StringComparer.Ordinal),
                    StringComparer.Ordinal);
            byAtomName = Structure.Atoms
                .GroupBy(a => a.PdbName())
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        }
        
        ////public MatchGraph<IAtom> GetMatchGraph()
        ////{
        ////    return WebChemistry.SiteBinder.Core.MatchGraph.Create(Structure, true, (Session as Session).IgnoreHydrogens);
        ////}

        public void SelectTopLevelAtoms(string name, List<string> bottomNames,  bool value, SelectionInfoType type)
        {
            if (value)
            {
                switch (type)
                {
                    case SelectionInfoType.ResidueName: for (int i = 0; i < bottomNames.Count; i++) SelectMany(ResidueNameGrouping[name][bottomNames[i]], value, false); break;
                    case SelectionInfoType.AtomName: for (int i = 0; i < bottomNames.Count; i++) SelectMany(AtomNameGrouping[name][bottomNames[i]], value, false); break;
                    case SelectionInfoType.AtomType: for (int i = 0; i < bottomNames.Count; i++) SelectMany(AtomTypeGrouping[name][bottomNames[i]], value, false); break;
                }
                UpdateSelectionString();
            }
            else
            {
                switch (type)
                {
                    case SelectionInfoType.ResidueName: SelectMany(byResidueName[name], value); break;
                    case SelectionInfoType.AtomName: SelectMany(byAtomName[name], value); break;
                    case SelectionInfoType.AtomType: SelectMany(byAtomType[name], value); break;
                }
            }
        }

        public void SelectBottomLevelAtoms(string topName, string bottomName, bool value, SelectionInfoType type)
        {
            switch (type)
            {
                case SelectionInfoType.ResidueName: SelectMany(ResidueNameGrouping[topName][bottomName], value); break;
                case SelectionInfoType.AtomName: SelectMany(AtomNameGrouping[topName][bottomName], value); break;
                case SelectionInfoType.AtomType: SelectMany(AtomTypeGrouping[topName][bottomName], value); break;
            }
        }

        public bool IsGroupSelected(string topName, string bottomName, SelectionInfoType type)
        {
            switch (type)
            {
                case SelectionInfoType.ResidueName: return ResidueNameGrouping[topName][bottomName].All(a => a.IsSelected);
                case SelectionInfoType.AtomName: return AtomNameGrouping[topName][bottomName].All(a => a.IsSelected);
                case SelectionInfoType.AtomType: return AtomTypeGrouping[topName][bottomName].All(a => a.IsSelected);
            }
            return false;
        }

        public void SelectByResidue(PdbResidueIdentifier id, bool value)
        {
            SelectMany(a => a.ResidueIdentifier() == id, value);
        }

        IAtom[] orderedAtoms;
        Dictionary<ElementSymbol, int> selectedCounts;
        public const string NoAtomsSelectedLabel = "No atoms selected";
        public void UpdateSelectionString()
        {
            var oldSS = SelectionString;

            int count = Structure.Atoms.Where(a => a.IsSelected).Count();

            SelectedCount = count;

            if (count > 0)
            {
                StringBuilder sb = new StringBuilder();

                int numSelected = 0;
                foreach (var c in selectedCounts.Keys.ToArray()) selectedCounts[c] = 0;
                for (int i = 0; i < orderedAtoms.Length; i++)
                {
                    var atom = orderedAtoms[i];
                    if (atom.IsSelected)
                    {
                        selectedCounts[atom.ElementSymbol] = selectedCounts[atom.ElementSymbol] + 1;
                        ++numSelected;
                    }
                }

                SelectionString = string.Join(" ", selectedCounts.Where(p => p.Value > 0).OrderBy(p => p.Key.ToString()).Select(p => p.Value.ToString() + p.Key.ToString()));
            }
            else
            {
                SelectionString = NoAtomsSelectedLabel;
            }

            if (oldSS != SelectionString)
            {
                isDirty = true;
                NotifyPropertyChanged("SelectionString");
                NotifyPropertyChanged("SelectedCount");
            }
        }

        void AtomPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ignoreAtomPropertyChanged) return;

            if (e.PropertyName.Equals("IsSelected", StringComparison.Ordinal)) UpdateSelectionString();
        }

        public void SelectMany(Func<IAtom, bool> predicate, bool isSelected)
        {
            if (SelectAtomsInternal(Structure.Atoms.Where(predicate), isSelected))
            {
                UpdateSelectionString();
            }
        }

        public void SelectMany(IList<IAtom> atoms, bool isSelected, bool update = true)
        {
            var changed = SelectAtomsInternal(atoms, isSelected);
            if (update && changed) UpdateSelectionString();
        }

        protected override bool SelectAtomsInternal(IEnumerable<IAtom> atoms, bool selected)
        {
            bool changed = false;
            ignoreAtomPropertyChanged = true;
            foreach (var a in atoms)
            {
                if (a.IsSelected != selected)
                {
                    a.IsSelected = selected;
                    changed = true;
                }
            }
            ignoreAtomPropertyChanged = false;
            return changed;
        }

        public void ClearVisual()
        {
            if (visual != null)
            {
                visual.Dispose();
                visual = null;
            }
        }

        public override void Dispose()
        {
            Structure.Atoms.ForEach(a => a.PropertyChanged -= AtomPropertyChanged);
        }

        protected override void OnCreate()
        {
            Structure.ToCentroidCoordinates();
            ResidueString = Structure.PdbResidues().CountedShortAminoNamesString;
            ResidueCount = Structure.PdbResidues().Count;
            //OrderedResidueString = Structure.PdbResidues().OrderedCondensedResidueString;
            Init();
            Structure.Atoms.ForEach(a => a.PropertyChanged += AtomPropertyChanged);
        }
    }
}
