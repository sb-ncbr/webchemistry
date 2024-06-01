using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Silverlight.Common.DataModel
{
    public enum ScriptingElementState
    {
        Empty,
        Executable,
        Executed,
        Error
    }

    public class ScriptElement : ObservableObject
    {
        private ScriptingElementState _state;
        public ScriptingElementState State
        {
            get
            {
                return _state;
            }

            set
            {
                if (_state == value) return;

                _state = value;
                NotifyPropertyChanged("State");
            }
        }

        private string _scriptText;
        public string ScriptText
        {
            get
            {
                return _scriptText;
            }

            set
            {
                if (_scriptText == value) return;

                _scriptText = value;
                NotifyPropertyChanged("ScriptText");
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }

            set
            {
                if (_errorMessage == value) return;

                _errorMessage = value;
                NotifyPropertyChanged("ErrorMessage");
            }
        }

        private ICommand _executeCommand;
        public ICommand ExecuteCommand
        {
            get
            {
                _executeCommand = _executeCommand ?? new RelayCommand(() => Execute());
                return _executeCommand;
            }
        }

        public object Result { get; set; }
        public string StdOut { get; set; }

        void Execute()
        {
            ScriptService.Default.Execute(this);
        }
    }
}
