namespace WebChemistry.Silverlight.Common.Services
{
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Services module for ninject.
    /// </summary>
    public class ServicesModule : Ninject.Modules.NinjectModule
    {

        public override void Load()
        {
            Bind<LogService>().ToConstant(LogService.Default);
            Bind<QueryService>().ToConstant(QueryService.Default);
            Bind<ComputationService>().ToConstant(ComputationService.Default);
            Bind<SelectionService>().ToConstant(SelectionService.Default);
            var ss = ScriptService.Default;
            Bind<ScriptService>().ToConstant(ss);
        }
    }
}
