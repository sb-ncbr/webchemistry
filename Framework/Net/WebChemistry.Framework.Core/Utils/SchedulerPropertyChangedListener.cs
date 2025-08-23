namespace WebChemistry.Framework.Core.Utils
{
    using System.ComponentModel;
    using System.Reactive.Concurrency;

    public class SchedulerPropertyChangedListener<T> 
        where T : INotifyPropertyChanged
    {
        public SchedulerPropertyChangedListener(IScheduler scheduler)
        {
            
        }
    }
}
