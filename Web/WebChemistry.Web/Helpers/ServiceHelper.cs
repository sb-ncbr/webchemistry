using WebChemistry.Platform.Server;
using WebChemistry.Platform.Services;

namespace WebChemistry.Web.Helpers
{
    public static class ServiceHelper
    {
        public static ServiceInfo PatternQuery { get { return ServerManager.MasterServer.Services.GetService("PatternQuery"); } }
    }
}