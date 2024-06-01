using GalaSoft.MvvmLight.Command;
using System.Linq;
using System.Windows.Input;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class GlobalSelectionViewModel : ObservableObject
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

        public bool IsGlobal { get { return true; } }

        Session session;
        
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
                        var selected = await SelectionService.Default.SelectAtoms(session, QueryString, IsAdditive, session.Structures);
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
                        session.Structures.ForEach(s => s.SelectAtoms(s.Structure.Atoms, false));                        
                        break;
                    }
            }
        }

        public GlobalSelectionViewModel(Session session)
        {
            this.session = session;
            IsAdditive = true;
        }
    }
}
