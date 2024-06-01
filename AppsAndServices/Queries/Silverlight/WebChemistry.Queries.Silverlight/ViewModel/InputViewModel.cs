using GalaSoft.MvvmLight.Command;
using Ninject;
using System.Windows.Input;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Silverlight.DataModel;

namespace WebChemistry.Queries.Silverlight.ViewModel
{
    public class InputViewModel
    {
        [Inject]
        public Session Session { get; set; }

        [Inject]
        public LogService Log { get; set; }

        private ICommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                _openCommand = _openCommand ?? new RelayCommand(() => Session.OpenStructures());
                return _openCommand;
            }
        }

        private ICommand _clearCommand;
        public ICommand ClearCommand
        {
            get
            {
                _clearCommand = _clearCommand ?? new RelayCommand(() => Session.Clear());
                return _clearCommand;
            }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                _exportCommand = _exportCommand ?? new RelayCommand(() => Session.ExportToZip());
                return _exportCommand;
            }
        }

        private ICommand _findMotivesCommand;
        public ICommand FindMotivesCommand
        {
            get
            {
                _findMotivesCommand = _findMotivesCommand ?? new RelayCommand(() => Session.DoQuery());
                return _findMotivesCommand;
            }
        }

        public InputViewModel()
        {

        }
    }
}
