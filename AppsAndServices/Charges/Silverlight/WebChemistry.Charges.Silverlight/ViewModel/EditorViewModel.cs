using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Windows.Input;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class EditorViewModel : ObservableObject
    {
        [Ninject.Inject]
        public Session Session { get; set; }

        [Ninject.Inject]
        public LogService Log { get; set; }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand = _cancelCommand ?? new RelayCommand(() =>
                {
                    ServiceLocator.Current.GetInstance<MainViewModel>().Mode = AppMode.Input;
                });
                return _cancelCommand;
            }
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand
        {
            get
            {
                _copyCommand = _copyCommand ?? new RelayCommand(() =>
                {
                    if (CopyFromSet == null) return;

                    var xml = CopyFromSet.ToXml();
                    var name = CurrentSet != null ? CurrentSet.Name : "Enter Name Here";
                    xml.Attribute("Name").SetValue(name);
                    CurrentSetXml = xml.ToString();
                });
                return _copyCommand;
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand = _saveCommand ?? new RelayCommand(() => Save());
                return _saveCommand;
            }
        }

        private EemParameterSet _copyFromSet;
        public EemParameterSet CopyFromSet
        {
            get
            {
                return _copyFromSet;
            }

            set
            {
                if (_copyFromSet == value) return;
                _copyFromSet = value;
                NotifyPropertyChanged("CopyFromSet");
            }
        }

        private EemParameterSet _currentSet;
        public EemParameterSet CurrentSet
        {
            get
            {
                return _currentSet;
            }

            set
            {
                if (value == null) CurrentSetXml = "";
                else CurrentSetXml = value.ToXml().ToString();

                if (_currentSet == value) return;
                
                _currentSet = value;
                NotifyPropertyChanged("CurrentSet");
            }
        }

        private string _currentSetXml;
        public string CurrentSetXml
        {
            get
            {
                return _currentSetXml;
            }

            set
            {
                if (_currentSetXml == value) return;

                _currentSetXml = value;
                NotifyPropertyChanged("CurrentSetXml");
            }
        }

        EemParameterSet Validate()
        {
            try
            {
                var xml = XElement.Parse(CurrentSetXml);
                var set = EemParameterSet.FromXml(xml);
                return set;
            }
            catch (XmlException e)
            {
                Log.Error("XML", "Line: {0}, Message: {1}", e.LineNumber, e.Message);
                Log.Info("The error is most likely a redundant or missing <, >, or \".");

                return null;
            }
            catch (Exception e)
            {
                Log.Error("Parameter Sets", e.Message);
                return null;
            }
        }

        void Save()
        {
            var set = Validate();
            if (set != null)
            {
                Session.SetManager.Update(set);
                Log.Message("Set '{0}' was saved.", set.Name);
                Session.CurrentSet = set;
                Session.ActiveSets.Where(s => s.Set.Equals(set)).ForEach(s => s.Set = set);

                ServiceLocator.Current.GetInstance<MainViewModel>().Mode = AppMode.Input;
            }
        }
    }
}
