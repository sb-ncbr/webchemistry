using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using Microsoft.Practices.ServiceLocation;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Windows.Media;
using System.Linq;
using System;
using System.Windows.Controls;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Windows;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.SiteBinder.Silverlight.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        static Color FromHex(string hex)
        {
            var ec = ElementColor.Parse(hex);
            return Color.FromArgb(255, ec.R, ec.G, ec.B);
        }

        static string[] hexColors = new string[] { "#FF1417", "#FF6611", "#AACC22", "#92A77E", "#0088CC", "#175279", "#DDBB33", "#A9834B", "#AA6688", "#767676" };

        static Color[] colors = hexColors.Select(c => FromHex(c)).ToArray();
        // new Color[] 
        //    {     
            


        //        Color.FromArgb(255, 192, 0, 0),
        //        Color.FromArgb(255, 255, 0, 0),
        //        Color.FromArgb(255, 255, 192, 0),
        //        Color.FromArgb(255, 255, 255, 0),
        //        Color.FromArgb(255, 146, 208, 0),
        //        Color.FromArgb(255, 0, 176, 0),
        //        Color.FromArgb(255, 0, 176, 240),
        //        Color.FromArgb(255, 0, 112, 192),
        //        Color.FromArgb(255, 0, 32, 96),
        //        Color.FromArgb(255, 112, 48, 160),          
        //        Colors.White, 
        //        Colors.Gray,
        //        Colors.Black
        //    };
        public static Color[] ColorPallete { get { return colors; } }
        
        private ICommand _changeColorCommand;
        public ICommand ChangeColorCommand
        {
            get
            {
                _changeColorCommand = _changeColorCommand ?? new RelayCommand<Color?>(c =>
                {
                    InputViewModel.SelectedColor = null;
                    ResultViewModel.SelectedColor = null;

                    if (!c.HasValue) return;

                    Session.SelectedStructures.ForEach(m => m.Color = c.Value);
                });
                return _changeColorCommand;
            }
        }

        private ICommand _selectAllStructuresCommand;
        public ICommand SelectAllStructuresCommand
        {
            get
            {
                _selectAllStructuresCommand = _selectAllStructuresCommand ?? new RelayCommand(() => Session.Select(Session.Structures));
                return _selectAllStructuresCommand;
            }
        }

        private ICommand _selectNoStructuresCommand;
        public ICommand SelectNoStructuresCommand
        {
            get
            {
                _selectNoStructuresCommand = _selectNoStructuresCommand ?? new RelayCommand(() => Session.Unselect(Session.SelectedStructures));
                return _selectNoStructuresCommand;
            }
        }

        private ICommand _removeSelectedCommand;
        public ICommand RemoveSelectedCommand
        {
            get
            {
                _removeSelectedCommand = _removeSelectedCommand ?? new RelayCommand(() => Session.RemoveSelected());
                return _removeSelectedCommand;
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

        [Ninject.Inject]
        public LogService Log { get; set; }

        [Ninject.Inject]
        public InputViewModel InputViewModel { get; set; }

        [Ninject.Inject]
        public ResultViewModel ResultViewModel { get; set; }

        [Ninject.Inject]
        public Session Session { get; set; }
        
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
                        ServiceLocator.Current.GetInstance<MainPage>().GoToInputTab();
                        break;
                    case "sample":
                        Session.LoadWorkspace(() => Application.GetResourceStream(new Uri("sample.sws", UriKind.Relative)).Stream, 20000);
                        ServiceLocator.Current.GetInstance<MainPage>().GoToInputTab();
                        break;
                    case "clear":
                        Session.Clear();
                        ServiceLocator.Current.GetInstance<MainPage>().GoToInputTab();
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
            
            //this.Log.AddEntry("Welcome to SiteBinder " + SiteBinder.Core.SiteBinderVersion.Version + "!");

            //this.InputViewModel = ServiceLocator.Current.GetInstance<InputViewModel>();
        }
    }
}