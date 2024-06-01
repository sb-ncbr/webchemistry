using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Framework.Core;
using System.Reactive.Concurrency;

namespace WebChemistry.Silverlight.Common.Services
{
    public class ComputationService : ObservableObject
    {
        static ComputationService defaultSvc = new ComputationService();
        public static ComputationService Default
        {
            get { return defaultSvc; }
        }

        private ComputationProgress _progress;
        public ComputationProgress Progress
        {
            get
            {
                return _progress;
            }

            set
            {
                if (_progress == value) return;

                _progress = value;
                NotifyPropertyChanged("Progress");
            }
        }

        private string _elapsedString;
        public string ElapsedString
        {
            get
            {
                return _elapsedString;
            }

            set
            {
                if (_elapsedString == value) return;

                _elapsedString = value;
                NotifyPropertyChanged("ElapsedString");
            }
        }

        private Visibility _visibility = Visibility.Collapsed;
        public Visibility Visibility
        {
            get
            {
                return _visibility;
            }

            set
            {
                if (_visibility == value) return;

                _visibility = value;
                NotifyPropertyChanged("Visibility");
            }
        }

        private Visibility _timerVisibility;
        public Visibility TimerVisibility
        {
            get
            {
                return _timerVisibility;
            }

            set
            {
                if (_timerVisibility == value) return;

                _timerVisibility = value;
                NotifyPropertyChanged("TimerVisibility");
            }
        }

        private ICommand _abortCommand;
        public ICommand AbortCommand
        {
            get
            {
                _abortCommand = _abortCommand ?? new RelayCommand(() =>
                {
                    if (Progress != null && !Progress.CancelRequested) Progress.RequestCancel();
                });
                return _abortCommand;
            }
        }

        IDisposable elapsedDisposer;
        DateTime started = DateTime.Now;
        int depth = 0;

        public ComputationProgress Start()
        {
            depth++;

            if (Progress != null) return Progress;

            TimerVisibility = System.Windows.Visibility.Visible;
            Progress = new ComputationProgress { ObserverScheduler = new DispatcherScheduler(Deployment.Current.Dispatcher) };
            this.Visibility = System.Windows.Visibility.Visible;
            ElapsedString = "0.0s";
            started = DateTime.Now;
            elapsedDisposer = Observable.Interval(TimeSpan.FromSeconds(0.01))
                    .ObserveOnDispatcher()
                    .Subscribe(t => ElapsedString = (DateTime.Now - started).TotalSeconds.ToStringInvariant("0.00") + "s");

            return Progress;
        }

        public TimeSpan Elapsed { get { return DateTime.Now - started; } }

        public TimeSpan End()
        {
            depth--;

            if (depth == 0)
            {
                if (elapsedDisposer != null) elapsedDisposer.Dispose();
                this.Visibility = System.Windows.Visibility.Collapsed;
                Progress = null;
                return Elapsed;
            }
            else return Elapsed;
        }

        public static string GetElapasedTimeString(TimeSpan elapsed)
        {
            return elapsed.Days > 0 ? elapsed.ToString(@"d\.hh\:mm\:ss") : elapsed.ToString(@"hh\:mm\:ss");
        }
        
        //public async void Run(Computation computation)
        //{
        //    try
        //    {
        //        currentComputation = computation;
        //        this.Progress = computation.Progress;
        //        this.Visibility = System.Windows.Visibility.Visible;
        //        ElapsedString = "0.0s";
        //        var started = DateTime.Now;
        //        var elapsedObserevable = Observable.Interval(TimeSpan.FromSeconds(0.01))
        //            .ObserveOnDispatcher()
        //            .Subscribe(t => ElapsedString = (DateTime.Now - started).TotalSeconds.ToStringInvariant("0.00") + "s");
        //        computation.ObservedOn(new DispatcherScheduler(Deployment.Current.Dispatcher));
        //        await computation;
        //        elapsedObserevable.Dispose();
        //    }
        //    finally
        //    {
        //        this.Visibility = System.Windows.Visibility.Collapsed;
        //        currentComputation = null;
        //    }
        //}
    }
}
