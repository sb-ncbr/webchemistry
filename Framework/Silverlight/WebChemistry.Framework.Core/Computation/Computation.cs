// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    /// <summary>
    /// Represent a computation with no result (represented by System.Unit :)).
    /// Also contains static functions to create (relay) computations.
    /// </summary>
    public abstract class Computation : Computation<Unit>
    {
        static IScheduler _defaultScheduler = Scheduler.CurrentThread;

        /// <summary>
        /// Default scheduler to use for observing Computation events.
        /// </summary>
        public static IScheduler DefaultScheduler { get { return _defaultScheduler; } set { _defaultScheduler = value; } }

        /// <summary>
        /// Creates a relay computation. Ignores progress tracking.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Computation<TResult> Create<TResult>(Func<TResult> function)
        {
            return new RelayComputation<TResult>(function);
        }

        /// <summary>
        /// Creates a relay computation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Computation<TResult> Create<TResult>(Func<ComputationProgress, TResult> function)
        {
            return new RelayComputation<TResult>(function);
        }

        /// <summary>
        /// Creates a relay computation. Ignores progress tracking.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Computation Create(Action action)
        {
            return new RelayComputation(action);
        }

        /// <summary>
        /// Creates a relay computation.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Computation Create(Action<ComputationProgress> action)
        {
            return new RelayComputation(action);
        }
    }

    /// <summary>
    /// An abstract class representing a computation with progress tracking, exception handling and the sorts.
    /// Also, Fluent(TM).
    /// </summary>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class Computation<TResult> : IComputation<TResult, ProgressTick>
    {
        /// <summary>
        /// Empty computation.
        /// </summary>
        public static readonly Computation<TResult> Empty = Computation.Create<TResult>(_ => default(TResult));

        object syncRoot = new object();

        bool runCalled;

        bool cancelled = false;
        IScheduler executedOn = Scheduler.Default;
        ComputationProgress progress;
        List<Action<ProgressTick>> statusReactions = new List<Action<ProgressTick>>();
        List<Action> cancelledReactions = new List<Action>();
        List<Action> startedReactions = new List<Action>();
        List<Action> finishedReactions = new List<Action>();
        List<Action<Exception>> exceptionReactions = new List<Action<Exception>>();
        List<Action<TResult>> completedReactions = new List<Action<TResult>>();

        /// <summary>
        /// Progress tracking.
        /// </summary>
        public ComputationProgress Progress
        {
            get { return progress; }
        }

        /// <summary>
        /// To make the life easier when Cancel is called.
        /// </summary>
        public bool IsCancelled { get { return cancelled; } }

        /// <summary>
        /// Function to be overridden by child types...
        /// </summary>
        /// <returns></returns>
        protected abstract TResult OnRun();

        /// <summary>
        /// Runs the computation asynchronously.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is thrown if RunAsync is called more than once.</exception>
        /// <param name="onCompleted"></param>
        /// <param name="onCancelled"></param>
        /// <param name="onError"></param>
        public void RunAsync(Action<TResult> onCompleted, Action onCancelled, Action<Exception> onError)
        {
            WhenCompleted(onCompleted);
            WhenCancelled(onCancelled);
            WhenError(onError);
            RunAsync();
        }

        /// <summary>
        /// Runs the computation asynchronously.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is thrown if RunAsync is called more than once.</exception>
        public void RunAsync()
        {
            new Func<TResult>(RunSynchronously).ToAsync(executedOn)().Subscribe();
        }

        /// <summary>
        /// Runs the computation synchronously.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is thrown if RunSynchronously is called more than once.</exception>
        /// <returns>Result of the computation.</returns>
        public TResult RunSynchronously()
        {
            if (runCalled)
            {
                throw new InvalidOperationException("The computation can only be executed once.");
            }
            runCalled = true;
            return RunAndDisposeObservers();
        }

        void OnStarted()
        {
            progress.Started();
            if (Progress.ObserverScheduler != null)
            {
                foreach (var r in startedReactions) Progress.ObserverScheduler.Schedule(r);
            }
            else
            {
                foreach (var r in startedReactions) r();
            }
        }

        void OnCompleted(TResult result)
        {
            if (Progress.ObserverScheduler != null)
            {
                foreach (var r in completedReactions) Progress.ObserverScheduler.Schedule(() => r(result));
            }
            else
            {
                foreach (var r in completedReactions) r(result);
            }
            OnFinished();
        }

        void OnCancelled()
        {
            if (Progress.ObserverScheduler != null)
            {
                foreach (var r in cancelledReactions) Progress.ObserverScheduler.Schedule(r);
            }
            else
            {
                foreach (var r in cancelledReactions) r();
            }
            OnFinished(false);
        }

        void OnError(Exception e)
        {
            progress.Exception();
            if (Progress.ObserverScheduler != null)
            {
                foreach (var r in exceptionReactions) Progress.ObserverScheduler.Schedule(() => r(e));
            }
            else
            {
                foreach (var r in exceptionReactions) r(e);
            }
            OnFinished(false);
        }

        void OnFinished(bool callFinishedOnProgress = true)
        {
            if (callFinishedOnProgress) progress.Finished();

            if (Progress.ObserverScheduler != null)
            {
                foreach (var r in finishedReactions) Progress.ObserverScheduler.Schedule(r);
            }
            else
            {
                foreach (var r in finishedReactions) r();
            }
        }

        void DisposeObservers()
        {
            lock (syncRoot)
            {
                statusReactions.Clear();
                completedReactions.Clear();
                exceptionReactions.Clear();
                cancelledReactions.Clear();
                startedReactions.Clear();
                finishedReactions.Clear();
            }
        }

        TResult RunAndDisposeObservers()
        {
            try
            {
                if (Progress.ObserverScheduler != null)
                {
                    foreach (var r in statusReactions) progress.Status.ObserveOn(Progress.ObserverScheduler).Subscribe(r);
                }
                else
                {
                    foreach (var r in statusReactions) progress.Status.Subscribe(r);
                }

                OnStarted();
                var result = OnRun();

                if (!cancelled)
                {
                    OnCompleted(result);
                    return result;
                }
            }
            catch (ComputationCancelledException)
            {
                return default(TResult);
            }
            catch (Exception e)
            {
                OnError(e);
                return default(TResult);
            }
            finally
            {
                DisposeObservers();
            }

            return default(TResult);
        }

        //static IScheduler GetScheduler(ExecuteOn on)
        //{
        //    switch (on)
        //    {
        //        case ExecuteOn.NewThread: return Scheduler.NewThread;
        //        case ExecuteOn.ThreadPool: return Scheduler.ThreadPool;
        //        //case ExecuteOn.UI: return Scheduler.Dispatcher;
        //        case ExecuteOn.CurrentThread: return Scheduler.CurrentThread;
        //        default: return null;
        //    }
        //}

        /// <summary>
        /// Cancel the computation.
        /// </summary>
        public void Cancel()
        {
            if (!Progress.CanCancel) return;

            lock (syncRoot)
            {
                this.cancelled = true;

                OnCancelled();
                Progress.RequestCancel();
                DisposeObservers();
            }
        }

        /// <summary>
        /// Sets the scheduler on which the ObservedBy, ReactsOnCancellationBy, ReactsOnExceptionBy are executed.
        /// This overrides the previous scheduler set by this function.
        /// Default value is Scheduler.Dispatcher.
        /// </summary>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public Computation<TResult> ObservedOn(IScheduler scheduler)
        {
            if (Progress.IsComputing) throw new InvalidOperationException("Cannot change the scheduler because the computation is already running.");
            Progress.ObserverScheduler = scheduler;
            return this;
        }

        ///// <summary>
        ///// Pretty much equivalent to ObservedOn(IScheduler), only perhaps more intuitive.
        ///// This overrides the previous scheduler set by this function.
        ///// </summary>
        ///// <param name="on"></param>
        ///// <returns></returns>
        //public Computation<TResult> ObservedOn(ExecuteOn on = ExecuteOn.UI)
        //{
        //    if (Progress.IsComputing) throw new InvalidOperationException("Cannot changed the scheduler because the computation is already running.");
        //    Progress.ObserverScheduler = GetScheduler(on);
        //    return this;
        //}

        /// <summary>
        /// Changes the scheduler on which the computation is executed on.
        /// This overrides the previous scheduler set by this function.
        /// Default is Scheduler.ThreadPool.
        /// This does not affect RunSynchronously.
        /// </summary>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public Computation<TResult> ExecutedOn(IScheduler scheduler)
        {
            if (Progress.IsComputing) throw new InvalidOperationException("Cannot changed the scheduler because the computation is already running.");
            this.executedOn = scheduler;
            return this;
        }

        ///// <summary>
        ///// Pretty much equivalent to ExecutedOn(IScheduler), only perhaps more intuitive.
        ///// This overrides the previous scheduler set by this function.
        ///// </summary>
        ///// <param name="on"></param>
        ///// <returns></returns>
        //public Computation<TResult> ExecutedOn(ExecuteOn on = ExecuteOn.ThreadPool)
        //{
        //    if (Progress.IsComputing) throw new InvalidOperationException("Cannot changed the scheduler because the computation is already running.");
        //    this.executedOn = GetScheduler(on);
        //    return this;
        //}

        /// <summary>
        /// Observes the computation's progress.
        /// Observed on the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenProgressUpdated(Action<ProgressTick> action)
        {
            statusReactions.Add(action);
            return this;
        }

        /// <summary>
        /// Executes an action if the computation is cancelled.
        /// Executed by the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenCancelled(Action action)
        {
            cancelledReactions.Add(action);
            return this;
        }

        /// <summary>
        /// Executes an action if an exception (other than OperationCancelledException) is executed.
        /// Executed by the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenError(Action<Exception> action)
        {
            exceptionReactions.Add(action);
            return this;
        }

        /// <summary>
        /// Executes an action when(if) the computation is completed.
        /// Executed by the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenCompleted(Action<TResult> action)
        {
            completedReactions.Add(action);
            return this;
        }

        /// <summary>
        /// Executed when the computation starts.
        /// Executed by the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenStarted(Action action)
        {
            startedReactions.Add(action);
            return this;
        }

        /// <summary>
        /// Executed after the computation finishes.
        /// Executed by the scheduler set by ObserveOn function.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Computation<TResult> WhenFinished(Action action)
        {
            finishedReactions.Add(action);
            return this;
        }

        /// <summary>
        /// C'tor
        /// </summary>
        protected Computation()
        {
            if (Computation.DefaultScheduler == null)
            {
                throw new InvalidOperationException("Computation: The default scheduler is not initialized! Please use Computation.DefaultScheduler property to initialize it.");
            }

            progress = new ComputationProgress();
            progress.ObserverScheduler = Computation.DefaultScheduler;
            //try
            //{
            //    //progress.ObserverScheduler = Scheduler.Dispatcher;
            //}
            //catch
            //{
            //    progress.ObserverScheduler = null;
            //}
        }
    }
}
