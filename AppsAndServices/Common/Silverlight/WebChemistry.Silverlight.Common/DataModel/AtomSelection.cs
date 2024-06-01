using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Silverlight.Common.Services;
using System.Reactive.Subjects;

namespace WebChemistry.Silverlight.Common.DataModel
{
    public class AtomSelection<TStructure> : ObservableObject
         where TStructure : StructureWrapBase<TStructure>, new()
    {
        private ICommand _selectCommand;
        public ICommand SelectCommand
        {
            get
            {
                _selectCommand = _selectCommand ?? new RelayCommand<string>(action => Select(action));
                return _selectCommand;
            }
        }

        public bool IsGlobal { get { return false; } }

        public TStructure Structure { get; private set; }
        SessionBase<TStructure> session;


        Subject<System.Reactive.Unit> selectionChanged = new Subject<System.Reactive.Unit>();

        /// <summary>
        /// Raised after using SelectAtoms function.
        /// </summary>
        public IObservable<System.Reactive.Unit> Changed { get { return selectionChanged; } }

        internal void RaiseSelectionChanged()
        {
            selectionChanged.OnNext(System.Reactive.Unit.Default);
        }
                
        private bool _isAdditive;
        public bool IsAdditive
        {
            get
            {
                return _isAdditive;
            }

            set
            {
                if (_isAdditive == value) return;

                _isAdditive = value;
                NotifyPropertyChanged("IsAdditive");
            }
        }

        private string _selectionString;
        public string SelectionString
        {
            get
            {
                return _selectionString;
            }

            set
            {
                if (_selectionString == value) return;

                _selectionString = value;
                NotifyPropertyChanged("SelectionString");
            }
        }

        private int _selectedCount;
        public int SelectedCount
        {
            get
            {
                return _selectedCount;
            }

            set
            {
                if (_selectedCount == value) return;

                _selectedCount = value;
                NotifyPropertyChanged("SelectedCount");
            }
        }

        private bool _queryVisible;
        public bool QueryVisible
        {
            get
            {
                return _queryVisible;
            }

            set
            {
                if (_queryVisible == value) return;

                _queryVisible = value;
                NotifyPropertyChanged("QueryVisible");
            }
        }

        private string _queryString;
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

        async void Select(string action)
        {
            action = action.ToLowerInvariant();

            switch (action)
            {
                case "beginadd":
                    QueryVisible = !QueryVisible;
                    break;
                case "add":
                    {
                        var selected = await SelectionService.Default.SelectAtoms(session, QueryString, IsAdditive, Structure.ToSingletonArray());
                        if (selected)
                        {
                            QueryString = "";
                        }
                        break;
                    }
                case "cancel":
                    QueryVisible = false;
                    break;
                case "clear":
                    {
                        Structure.SelectAtoms(Structure.Structure.Atoms, false);
                        Update();
                        break;
                    }
            }
        }

        ///// <summary>
        ///// Update the selection. Later, there will be an event and list of atoms changed.
        ///// </summary>
        //public void Add(IEnumerable<IAtom> newAtoms)
        //{
        //    newAtoms.ForEach(a => a.IsSelected = true);
        //    var sel = Structure.GetSelectionString();
        //    SelectedCount = sel.Item1;
        //    SelectionString = sel.Item2;
        //}

        public void Update()
        {
            var sel = Structure.GetSelectionString();
            SelectedCount = sel.Item1;
            SelectionString = sel.Item2;
        }

        //public void Clear()
        //{
        //    this.Structure.Structure.SelectAllAtoms(false);
        //    var sel = Structure.GetSelectionString();
        //    SelectedCount = sel.Item1;
        //    SelectionString = sel.Item2;
        //}

        public AtomSelection(TStructure structure, SessionBase<TStructure> session)
        {
            this.Structure = structure;
            this.session = session;
            var sel = structure.GetSelectionString();
            SelectedCount = sel.Item1;
            SelectionString = sel.Item2;
            IsAdditive = true;
        }
    }
}
