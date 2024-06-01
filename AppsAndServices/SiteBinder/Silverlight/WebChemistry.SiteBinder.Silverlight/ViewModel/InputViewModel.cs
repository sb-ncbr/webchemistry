using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using WebChemistry.Framework.Core;
using WebChemistry.SiteBinder.Silverlight.DataModel;

namespace WebChemistry.SiteBinder.Silverlight.ViewModel
{
    public class InputViewModel : ViewModelBase
    {
        LogService log;

        public Session Session { get; private set; }

        private ICommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                _openCommand = _openCommand ?? new RelayCommand(() => Session.OpenStructures());
                return _openCommand;
            }
        }
        
        private ICommand _selectGroupCommand;
        public ICommand SelectGroupCommand
        {
            get
            {
                _selectGroupCommand = _selectGroupCommand ?? new RelayCommand<System.Windows.Data.CollectionViewGroup>(g => Session.Select(g.Items.Select(x => x as StructureWrap)));
                return _selectGroupCommand;
            }
        }

        private ICommand _unselectGroupCommand;
        public ICommand UnselectGroupCommand
        {
            get
            {
                _unselectGroupCommand = _unselectGroupCommand ?? new RelayCommand<System.Windows.Data.CollectionViewGroup>(g => Session.Unselect(g.Items.Select(x => x as StructureWrap)));
                return _unselectGroupCommand;
            }
        }

        private ICommand _selectAtomsCommand;
        public ICommand SelectAtomsCommand
        {
            get
            {
                _selectAtomsCommand = _selectAtomsCommand ?? new RelayCommand<bool>(v => Session.SelectAllAtomsOnSelectedStructures(v, true));
                return _selectAtomsCommand;
            }
        }

        private ICommand _expandAtomSelectionCommand;
        public ICommand ExpandAtomSelectionCommand
        {
            get
            {
                _expandAtomSelectionCommand = _expandAtomSelectionCommand ?? new RelayCommand(() => Session.ExpandAtomSelectionOnSelectedStructures());
                return _expandAtomSelectionCommand;
            }
        }

        private ICommand _selectAtomsStarCommand;
        public ICommand SelectAtomsStarCommand
        {
            get
            {
                _selectAtomsStarCommand = _selectAtomsStarCommand ?? new RelayCommand(() => Session.SelectAllAtomsOnSelectedStructures(true, false));
                return _selectAtomsStarCommand;
            }
        }

        private ICommand _invertAtomSelectionCommand;
        public ICommand InvertAtomSelectionCommand
        {
            get
            {
                _invertAtomSelectionCommand = _invertAtomSelectionCommand ?? new RelayCommand(() => Session.InvertAtomSelection());
                return _invertAtomSelectionCommand;
            }
        }

        private ICommand _buildConnectedCommand;
        public ICommand BuildConnectedCommand
        {
            get
            {
                _buildConnectedCommand = _buildConnectedCommand ?? new RelayCommand(() => Session.MakeConnected());
                return _buildConnectedCommand;
            }
        }

        private ICommand _invertStructureSelectionCommand;
        public ICommand InvertStructureSelectionCommand
        {
            get
            {
                _invertStructureSelectionCommand = _invertStructureSelectionCommand ?? new RelayCommand(() =>
                {
                    var newSelection = Session.Structures.Where(s => !s.IsSelected).ToArray();
                    Session.Unselect(Session.SelectedStructures);
                    Session.Select(newSelection);
                });
                return _invertStructureSelectionCommand;
            }
        }

        private Color? _selectedColor;
        public Color? SelectedColor
        {
            get
            {
                return _selectedColor;
            }

            set
            {
                if (value == _selectedColor) return;

                _selectedColor = value;
                RaisePropertyChanged("SelectedColor");
            }
        }
        
        public InputViewModel(Session session, LogService log)
        {
            this.Session = session;
            this.log = log;
        }
    }
}
