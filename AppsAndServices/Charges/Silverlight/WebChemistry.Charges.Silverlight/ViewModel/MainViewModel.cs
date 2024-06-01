using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public enum AppMode
    {
        Input,
        Result,
        EditSet,
        Analyze,
        Visualization
    }

    public class MainViewModel : ViewModelBase
    {
        static Color FromHex(string hex)
        {
            var ec = ElementColor.Parse(hex);
            return Color.FromArgb(255, ec.R, ec.G, ec.B);
        }

        static string[] hexColors = new string[] { "#FF1417", "#FF6611", "#AACC22", "#92A77E", "#0088CC", "#175279", "#DDBB33", "#A9834B", "#AA6688", "#767676" };

        static Color[] colors = hexColors.Select(c => FromHex(c)).ToArray();
        public static Color[] ColorPallete { get { return colors; } }

        private ICommand _selectSetGroupCommand;
        public ICommand SelectSetGroupCommand
        {
            get
            {
                _selectSetGroupCommand = _selectSetGroupCommand ?? new RelayCommand<CollectionViewGroup>(g => 
                    {
                        if (g.IsBottomLevel)
                        {
                            g.Items.ForEach(i => (i as IInteractive).IsSelected = true);
                        }
                        else
                        {
                            g.Items.ForEach(h =>
                                {
                                    (h as CollectionViewGroup).Items.ForEach(i => (i as IInteractive).IsSelected = true);
                                });
                        }
                    });
                return _selectSetGroupCommand;
            }
        }

        private ICommand _unselectSetGroupCommand;
        public ICommand UnselectSetGroupCommand
        {
            get
            {
                _unselectSetGroupCommand = _unselectSetGroupCommand ?? new RelayCommand<CollectionViewGroup>(g =>
                {
                    if (g.IsBottomLevel)
                    {
                        g.Items.ForEach(i => (i as IInteractive).IsSelected = false);
                    }
                    else
                    {
                        g.Items.ForEach(h =>
                        {
                            (h as CollectionViewGroup).Items.ForEach(i => (i as IInteractive).IsSelected = false);
                        });
                    }
                });
                return _unselectSetGroupCommand;
            }
        }

        private ICommand _clearSetSelectionCommand;
        public ICommand ClearSetSelectionCommand
        {
            get
            {
                _clearSetSelectionCommand = _clearSetSelectionCommand ?? new RelayCommand(() => Session.SetManager.Sets.ForEach(s => s.IsSelected = false));
                return _clearSetSelectionCommand;
            }
        }

        private ICommand _setCurrentSetCommand;
        public ICommand SetCurrentSetCommand
        {
            get
            {
                _setCurrentSetCommand = _setCurrentSetCommand ?? new RelayCommand<EemParameterSet>(set => Session.CurrentSet = set);
                return _setCurrentSetCommand;
            }
        }

        private ICommand _editSetCommand;
        public ICommand EditSetCommand
        {
            get
            {
                _editSetCommand = _editSetCommand ?? new RelayCommand<EemParameterSet>(set =>
                {
                    EditorViewModel.CurrentSet = set;
                    Mode = AppMode.EditSet;
                });
                return _editSetCommand;
            }
        }

        private ICommand _removeSetCommand;
        public ICommand RemoveSetCommand
        {
            get
            {
                _removeSetCommand = _removeSetCommand ?? new RelayCommand<EemParameterSet>(set =>
                {
                    if (EditorViewModel.CurrentSet == set) EditorViewModel.CurrentSet = null;
                    if (Session.CurrentSet == set) Session.CurrentSet = null;

                    Session.SetManager.Remove(set);
                    Session.ActiveSets.Where(s => s.Set == set).ToArray().ForEach(s => Session.ActiveSets.Remove(s));
                });
                return _removeSetCommand;
            }
        }

        private ICommand _newSetCommand;
        public ICommand NewSetCommand
        {
            get
            {
                _newSetCommand = _newSetCommand ?? new RelayCommand(() =>
                {
                    EditorViewModel.CurrentSet = EemParameterSet.NewSet();
                    Mode = AppMode.EditSet;
                });
                return _newSetCommand;
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

        private AppMode _mode;
        public AppMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode == value) return;

                _mode = value;
                RaisePropertyChanged("Mode");

                switch (value)
                {
                    case AppMode.Analyze:
                        AnalyzeViewModel.Show(true);
                        break;
                    case AppMode.Visualization:
                        VisualizationViewModel.Update();
                        break;
                    default:
                        VisualizationViewModel.Clear();
                        break;
                }
            }
        }

        [Ninject.Inject]
        public LogService Log { get; set; }
        
        [Ninject.Inject]
        public Session Session { get; set; }

        [Ninject.Inject]
        public InputViewModel InputViewModel { get; set; }

        [Ninject.Inject]
        public EditorViewModel EditorViewModel { get; set; }

        [Ninject.Inject]
        public AnalyzeViewModel AnalyzeViewModel { get; set; }

        [Ninject.Inject]
        public ResultViewModel ResultViewModel { get; set; }

        [Ninject.Inject]
        public VisualizationViewModel VisualizationViewModel { get; set; }

        void Workspace(string what)
        {
            try
            {
                switch (what.ToLower())
                {
                    case "save":
                        Log.Message("TODO");
                        //Session.SaveSession();
                        break;
                    case "load":
                        Log.Message("TODO");
                        //Session.LoadSession();
                        break;
                    case "sample":
                        Log.Message("TODO");
                        //Session.LoadWorkspace(() => Application.GetResourceStream(new Uri("sample.cws", UriKind.Relative)).Stream, 20000);
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