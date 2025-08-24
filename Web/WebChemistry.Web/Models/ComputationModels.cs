using WebChemistry.Platform.Computation;

namespace WebChemistry.Web.Models
{
    public class ComputationModel
    {
        public ComputationInfo Info { get; set; }
        public ComputationStatus Status { get; set; }
        public bool IsRunning { get; set; }

        public T GetCustomState<T>() where T : new() 
        { 
            return Status.GetJsonCustomState<T>(); 
        }
    }
}