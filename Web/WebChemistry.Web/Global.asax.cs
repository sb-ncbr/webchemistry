using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebChemistry.Platform.Server;
using WebChemistry.Web.Controllers;

namespace WebChemistry.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var serversConfig = @"c:\webchemservers.json";
            ServerManager.Init(serversConfig);
        }

        public void Application_End()
        {
            try
            {
                //System.IO.File.WriteAllText("e:/endevent.txt", "app1 endfired at " + DateTime.Now);
                WebChemistry.Web.Controllers.ValidatorDbController.DbInterfaceProvider.Dispose();
                foreach (var inst in PatternQueryController.ActiveExplorerInstances) inst.Value.Dispose();
                PatternQueryController.ActiveExplorerInstances.Clear();

                foreach (var inst in PatternQueryController.ActivePatternQueryResults) inst.Value.Dispose();
                PatternQueryController.ActivePatternQueryResults.Clear();
                //MotiveValidatorDatabaseInterface.Dispose();
                GC.Collect();
                //System.IO.File.WriteAllText("e:/endevent_disposed.txt", "app1 endfired at " + DateTime.Now);
            }
            catch
            { }
        }
    }
}