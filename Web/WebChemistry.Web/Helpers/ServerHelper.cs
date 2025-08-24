using WebChemistry.Platform.Server;

namespace WebChemistry.Web.Helpers
{
    public static class ServerHelper
    {
        public static PlatformServer Default
        {
            get { return ServerManager.GetPlatformServer("Default"); }
        }

        public static AppServer Atlas
        {
            get { return ServerManager.GetAppServer("Atlas"); }
        }
    }
}