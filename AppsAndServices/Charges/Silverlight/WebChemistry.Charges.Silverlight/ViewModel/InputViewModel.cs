namespace WebChemistry.Charges.Silverlight.ViewModel
{
    using GalaSoft.MvvmLight.Command;
    using Ninject;
    using System.Windows.Input;
    using WebChemistry.Charges.Silverlight.DataModel;
    using WebChemistry.Framework.Core;
    using WebChemistry.Silverlight.Common.Services;

    public class InputViewModel
    {
        [Inject]
        public LogService Log { get; set; }

        [Inject]
        public Session Session { get; set; }

        private ICommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                _openCommand = _openCommand ?? new RelayCommand(() => Session.OpenStructures());
                return _openCommand;
            }
        }
    }
}
