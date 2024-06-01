using Ninject;
using GalaSoft.MvvmLight;
using WebChemistry.Queries.Silverlight.DataModel;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Controls;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Queries.Silverlight.ViewModel
{
    public class MainViewModel : ViewModelBase
    {

        [Inject]
        public LogService Log { get; set; }

        [Inject]
        public InputViewModel InputViewModel { get; set; }

        [Inject]
        public Session Session { get; set; }

        private ICommand _removeCommand;
        public ICommand RemoveCommand
        {
            get
            {
                _removeCommand = _removeCommand ?? new RelayCommand<StructureWrap>(s => Session.Remove(s));
                return _removeCommand;
            }
        }

        private ICommand _workspaceCommand;
        public ICommand WorkspaceCommand
        {
            get
            {
                _workspaceCommand = _workspaceCommand ?? new RelayCommand<string>(what => Workspace(what));
                return _workspaceCommand;
            }
        }

        private ICommand _selectCommand;
        public ICommand SelectCommand
        {
            get
            {
                _selectCommand = _selectCommand ?? new RelayCommand<IInteractive>(what => what.IsSelected = !what.IsSelected);
                return _selectCommand;
            }
        }

        void LoadSample()
        {
            Log.Info("Sample comming soon...");
        }

        void Workspace(string what)
        {
            try
            {
                switch (what.ToLower())
                {
                    case "save":
                        Session.SaveSession();
                        break;
                    case "load":
                        Session.LoadSession();
                        break;
                    case "sample":
                        LoadSample();
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error("Workspace", e.Message);
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }
    }
}