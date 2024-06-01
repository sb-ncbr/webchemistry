using System.ComponentModel;

namespace WebChemistry.Framework.Visualization
{
    public class UpdateAsyncArgs
    {
        public BackgroundWorker Worker { get; set; }
        public DoWorkEventArgs WorkerArgs { get; set; }
        
        public int WorkerIndex { get; set; }
        public int WorkerCount { get; set; }

        public UpdateContext Context { get; set; }
    }
}