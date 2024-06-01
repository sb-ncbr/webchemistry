using System;
using System.Linq;
using System.Collections.Generic;
using WebChemistry.Framework.Core;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace WebChemistry.SiteBinder.Silverlight.DataModel
{
    public enum SelectionInfoType
    {
        ResidueName,
        AtomType,
        AtomName
    }

    public class MultipleSelectionInfo : SelectionInfo
    {
        public class BottomLevel : LevelBase
        {
            public Func<bool> GetSelected { get; set; }

            public bool AvoidFeedback = false;
            protected override void OnSelectedChanged()
            {
                if (AvoidFeedback) return;
                Selector(IsSelected);
            }

            public void Update()
            {
                AvoidFeedback = true;
                IsSelected = GetSelected();
                AvoidFeedback = false;
            }

            public BottomLevel(Action<bool> selector)
            {
                Selector = selector;
            }
        }

        public class TopLevel : LevelBase
        {
            public BottomLevel[] Groups { get; private set; }

            bool avoidFeedback = false;
            bool dontListen = false;
            protected override void OnSelectedChanged()
            {
                if (avoidFeedback) return;
                dontListen = true;
                foreach (var g in Groups)
                {
                    g.AvoidFeedback = true;
                    g.IsSelected = IsSelected;
                    g.AvoidFeedback = false;
                }
                dontListen = false;
                Selector(IsSelected);
            }

            public TopLevel(BottomLevel[] groups, Action<bool> selector)
            {
                Selector = selector;
                Groups = groups;
                groups.ForEach(g => g.ObserveInteractivePropertyChanged(this, (l, v) => 
                    {
                        if (l.dontListen) return;

                        l.avoidFeedback = true;
                        if (!v.IsSelected) l.IsSelected = false;
                        else l.IsSelected = l.Groups.All(h => h.IsSelected);

                        l.avoidFeedback = false;
                    }));

                avoidFeedback = true;
                IsSelected = groups.All(g => g.IsSelected);
                avoidFeedback = false;
            }

            public void Update()
            {
                dontListen = true;
                avoidFeedback = true;
                Groups.ForEach(g => g.Update());
                IsSelected = Groups.All(g => g.IsSelected);
                avoidFeedback = false;
                dontListen = false;
            }
        }

        public TopLevel[] Groups { get; set; }

        public override void Update()
        {
            Groups.ForEach(g => g.Update());
        }
    }

    public class SingleSelectionInfo : SelectionInfo
    {
        public class BottomLevel : LevelBase
        {
            public IAtom Atom { get; set; }

            bool avoidFeedback = false;
            protected override void OnSelectedChanged()
            {
                if (avoidFeedback) return;
                Selector(IsSelected);
            }

            public BottomLevel(IAtom atom)
            {
                Name = atom.PdbName() + " " + atom.PdbSerialNumber();
                Atom = atom;
                Selector = v => atom.IsSelected = v;
                IsSelected = atom.IsSelected;
                atom.ObserveInteractivePropertyChanged(this, (l, a) =>
                {
                    l.avoidFeedback = true;
                    l.IsSelected = a.IsSelected;
                    l.avoidFeedback = false;
                });
            }
        }

        public class TopLevel : LevelBase
        {
            public BottomLevel[] Groups { get; private set; }

            bool avoidFeedback = false;
            protected override void OnSelectedChanged()
            {
                if (avoidFeedback) return;
                Selector(IsSelected);
            }

            void UpdateSelected()
            {
                avoidFeedback = true;
                this.IsSelected = Groups.All(a => a.IsSelected);
                avoidFeedback = false;
            }

            public TopLevel(BottomLevel[] groups, Action<bool> selector)
            {
                Groups = groups;
                Selector = selector;
                IsSelected = groups.All(g => g.IsSelected);

                groups.ForEach(g => g.Atom.ObserveInteractivePropertyChanged(this, (l, _) => l.UpdateSelected()));
            }
        }

        public TopLevel[] Groups { get; set; }
    }

    public abstract class SelectionInfo
    {
        public static readonly SelectionInfo Empty = new SingleSelectionInfo { Groups = new SingleSelectionInfo.TopLevel[0] };

        public abstract class LevelBase : InteractiveObject
        {
            public string Name { get; set; }
            public Action<bool> Highlighter { get; set; }
            public Action<bool> Selector { get; protected set; }

            private ICommand _highlightCommand;
            public ICommand HighlightCommand
            {
                get
                {
                    _highlightCommand = _highlightCommand ?? new RelayCommand<bool>(v => Highlighter(v));
                    return _highlightCommand;
                }
            }
        }

        public virtual void Update() { }

        public static SelectionInfo Create(IList<StructureWrap> xs, Session sesssion, SelectionInfoType type)
        {
            if (xs.Count == 0) return SelectionInfo.Empty;

            if (xs.Count == 1)
            {
                var s = xs[0];

                Func<IAtom, string> topLevelName = null;

                switch (type)
                {
                    case SelectionInfoType.ResidueName: topLevelName = a => string.Format("{0} {2} {1}", a.PdbResidueName(),  !string.IsNullOrWhiteSpace(a.PdbChainIdentifier()) ? a.PdbChainIdentifier().ToString() : "", a.PdbResidueSequenceNumber()); break;
                    case SelectionInfoType.AtomName: topLevelName = a => a.PdbName(); break;
                    case SelectionInfoType.AtomType: topLevelName = a => a.ElementSymbol.ToString(); break;
                }

                var gs = s.Structure.Atoms
                    .GroupBy(a => topLevelName(a))
                    .Select(g => new SingleSelectionInfo.TopLevel(
                        groups: g.Select(a => new SingleSelectionInfo.BottomLevel(a)).ToArray(),
                        selector: v => s.SelectByResidue(g.First().ResidueIdentifier(), v))
                    {
                        Name = g.Key
                    });

                if (type != SelectionInfoType.ResidueName) gs = gs.OrderBy(g => g.Name);
                
                return new SingleSelectionInfo { Groups = gs.ToArray() };
            }

            Func<StructureWrap, Dictionary<string, Dictionary<string, IAtom[]>>> dictSource = null;
            switch (type)
            {
                case SelectionInfoType.ResidueName: dictSource = s => s.ResidueNameGrouping; break;
                case SelectionInfoType.AtomName: dictSource = s => s.AtomNameGrouping; break;
                case SelectionInfoType.AtomType: dictSource = dictSource = s => s.AtomTypeGrouping; break;
            }

            var pivot = dictSource(xs.MinBy(s => dictSource(s).Count)[0]);
            Dictionary<string, List<string>> names = new Dictionary<string, List<string>>();

            //var pivotArray = pivot.Select(v => new { v.Key, Value = v.Value.ToArray() }).ToArray();

            foreach (var t in pivot)
            {
                foreach (var b in t.Value)
                {
                    var reqLen = b.Value.Length;

                    bool ok = true;
                    for (int i = 0; i < xs.Count; i++)
                    {
                        var d = dictSource(xs[i]);
                        Dictionary<string, IAtom[]> atmsDict;
                        if (d.TryGetValue(t.Key, out atmsDict))
                        {
                            IAtom[] atms;
                            if (atmsDict.TryGetValue(b.Key, out atms))
                            {
                                if (atms.Length != reqLen) ok = false;
                            }
                            else ok = false;
                        }
                        else ok = false;
                    }

                    if (ok)
                    {
                        List<string> nms;
                        if (names.TryGetValue(t.Key, out nms)) nms.Add(b.Key);
                        else
                        {
                            names.Add(t.Key, new List<string> { b.Key });
                        }
                    }
                }
            }

            {
                var ls = sesssion;
                var lt = type;
                var lxs = xs;

                var gs = names
                    .Select(n => new MultipleSelectionInfo.TopLevel(
                        selector: v => ls.SelectTopLevelAtoms(n.Key, n.Value, lt, v),
                        groups: n.Value.Select(b => new MultipleSelectionInfo.BottomLevel(v => ls.SelectBottomLevelAtoms(n.Key, b, lt, v))
                            {
                                Name = b,
                                IsSelected = xs.All(s => s.IsGroupSelected(n.Key, b, lt)),
                                GetSelected = () => xs.All(s => s.IsGroupSelected(n.Key, b, lt))
                            }).ToArray())
                    {
                        Name = n.Key                        
                    })
                    .OrderBy(g => g.Name)
                    .ToArray();
                return new MultipleSelectionInfo { Groups = gs };
            }
        }
    }
}
