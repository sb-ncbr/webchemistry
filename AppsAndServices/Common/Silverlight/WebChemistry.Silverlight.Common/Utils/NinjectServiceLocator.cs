using Ninject;

namespace WebChemistry.Silverlight.Common
{
    public class NinjectCommonLocator : Microsoft.Practices.ServiceLocation.ServiceLocatorImplBase
    {
        IKernel kernel;

        protected override System.Collections.Generic.IEnumerable<object> DoGetAllInstances(System.Type serviceType)
        {
            return kernel.GetAll(serviceType);
        }

        protected override object DoGetInstance(System.Type serviceType, string key)
        {
            return kernel.Get(serviceType, key);
        }

        public NinjectCommonLocator(IKernel kernel)
        {
            this.kernel = kernel;
        }
    }
}
