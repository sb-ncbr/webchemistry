// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Concurrency;
    using System.Reactive.Subjects;

    /// <summary>
    /// Represents a computation progress.
    /// </summary>
    public sealed class ComputationProgress : INotifyPropertyChanged
    {
        /// <summary>
        /// A dummy instance that is not connected to any computation.
        /// </summary>
        public static readonly ComputationProgress DummyInstance = new ComputationProgress();

        static readonly string StartedStatusText = "Computing...";
        static readonly string FinishedStatusText = "Finished.";
        static readonly string ExceptionStatusText = "Exception occurred.";
        static readonly string CancelledStatusText = "Cancelled.";

        object syncRoot = new object();

        Subject<ProgressTick> statusObservable;
        int current, length;
        string statusText;
        bool isIndeterminate, isComputing, cancelRequested, canCancel = true;

        public bool CancelRequested { get { return cancelRequested; } }

        /// <summary>
        /// StatusText property.
        /// </summary>
        public string StatusText
        {
            get { return this.statusText; }
            private set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    if (!cancelRequested || value == CancelledStatusText) NotifyPropertyChanged("StatusText");
                }
            }
        }

        /// <summary>
        /// Current progress property.
        /// </summary>
        public int Current
        {
            get { return this.current; }
            private set
            {
                if (this.current != value)
                {
                    this.current = value;
                    if (!cancelRequested) NotifyPropertyChanged("Current");
                }
            }
        }

        /// <summary>
        /// Computation length property.
        /// </summary>
        public int Length
        {
            get { return this.length; }
            private set
            {
                if (this.length != value)
                {
                    this.length = value;
                    if (!cancelRequested) NotifyPropertyChanged("Length");
                }
            }
        }

        /// <summary>
        /// IsIndeterminate property.
        /// </summary>
        public bool IsIndeterminate
        {
            get { return this.isIndeterminate; }
            private set
            {
                if (this.isIndeterminate != value)
                {
                    this.isIndeterminate = value;
                    if (!cancelRequested) NotifyPropertyChanged("IsIndeterminate");
                }
            }
        }

        /// <summary>
        /// IsComputing property.
        /// </summary>
        public bool IsComputing
        {
            get { return this.isComputing; }
            private set
            {
                if (this.isComputing != value)
                {
                    this.isComputing = value;
                    NotifyPropertyChanged("IsComputing");
                }
            }
        }

        /// <summary>
        /// CanCancel property.
        /// </summary>
        public bool CanCancel
        {
            get { return this.canCancel; }
            private set
            {
                if (this.canCancel != value)
                {
                    this.canCancel = value;
                    if (!cancelRequested) NotifyPropertyChanged("CanCancel");
                }
            }
        }

        /// <summary>
        /// Cancellation support.
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            if (cancelRequested)
            {
                UpdateStatus(CancelledStatusText);
                if (IsIndeterminate)
                {
                    UpdateProgress(0, 1);
                }
                throw new ComputationCancelledException();
            }
        }

        /// <summary>
        /// Update the status.
        /// </summary>
        /// <param name="statusText"></param>
        /// <param name="isIndeterminate"></param>
        /// <param name="currentProgress"></param>
        /// <param name="maxProgress"></param>
        /// <param name="canCancel"></param>
        public void Update(
            string statusText = null,
            bool? isIndeterminate = null,
            int? currentProgress = null,
            int? maxProgress = null,
            bool? canCancel = null)
        {
            if (statusText != null) StatusText = statusText;
            if (isIndeterminate != null) IsIndeterminate = isIndeterminate.Value;
            if (currentProgress != null) Current = currentProgress.Value;
            if (maxProgress != null) Length = maxProgress.Value;
            if (canCancel != null) CanCancel = canCancel.Value;
            Update();
        }

        /// <summary>
        /// Updates the status text of the computation.
        /// </summary>
        /// <param name="status">Status text.</param>
        public void UpdateStatus(string status)
        {
            StatusText = status;
            Update();
        }

        /// <summary>
        /// Updates the current progress of the computation.        
        /// If Current is greater than Length, then Current = Length.
        /// </summary>
        /// <param name="current">Current computation progress.</param>
        public void UpdateProgress(int current)
        {
            UpdateProgress(current, this.length);
        }

        /// <summary>
        /// Updates the length of the computation.
        /// If the length is non-zero, IsIndetermate is set to false.
        /// </summary>
        /// <param name="length"></param>
        public void UpdateLength(int length)
        {
            UpdateProgress(this.current, length);
        }

        /// <summary>
        /// Determines if the computation can currently track progress.
        /// </summary>
        /// <param name="isIndeterminate"></param>
        public void UpdateIsIndeterminate(bool isIndeterminate = false)
        {
            IsIndeterminate = isIndeterminate;
            if (isIndeterminate) UpdateLength(0);
            Update();
        }

        /// <summary>
        /// Combines UpdateProgress(current) and UpdateLength(length)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="length"></param>
        public void UpdateProgress(int current, int length)
        {
            IsIndeterminate = length == 0;
            if (current > length) Current = length;
            else Current = current;
            Length = length;
            Update();
        }

        /// <summary>
        /// Determines whether the computation can be cancelled at the given time.
        /// </summary>
        /// <param name="canCancel"></param>
        public void UpdateCanCancel(bool canCancel)
        {
            CanCancel = canCancel;
            Update();
        }

        void Update()
        {
            this.statusObservable.OnNext(new ProgressTick(statusText, isIndeterminate, current, length, canCancel));
        }

        /// <summary>
        /// Used by Computation object.
        /// </summary>
        internal IObservable<ProgressTick> Status
        {
            get { return statusObservable; }
        }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>
        internal void Started()
        {
            IsComputing = true;
            UpdateStatus(StartedStatusText);
        }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>
        internal void Finished()
        {
            IsComputing = false;

            if (IsIndeterminate) UpdateProgress(1, 1);
            else Current = length;

            UpdateStatus(FinishedStatusText);
            this.statusObservable.OnCompleted();
        }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>
        internal void Exception()
        {
            IsComputing = false;

            if (IsIndeterminate) UpdateProgress(0, 1);
            UpdateStatus(ExceptionStatusText);
            this.statusObservable.OnCompleted();
        }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>
        internal void Cancelled()
        {
            IsComputing = false;
            this.statusObservable.OnCompleted();
        }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>
        public void RequestCancel()
        {
            lock (syncRoot)
            {
                cancelRequested = true;
            }

            //Cancelled();
        }

        /// <summary>
        /// Set by Computation object.
        /// </summary>
        public IScheduler ObserverScheduler { get; set; }

        /// <summary>
        /// Used by the Computation object.
        /// </summary>    
        public ComputationProgress()
        {
            IsIndeterminate = true;
            this.statusObservable = new Subject<ProgressTick>();
        }

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                if (ObserverScheduler != null)
                {
                    ObserverScheduler.Schedule(() => handler(this, new PropertyChangedEventArgs(propertyName)));
                }
                else
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}