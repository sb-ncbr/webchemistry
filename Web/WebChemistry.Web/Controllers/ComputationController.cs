using System.Web.Mvc;
using WebChemistry.Platform.Computation;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;
using WebChemistry.Platform.Server;

namespace WebChemistry.Web.Controllers
{
    [MustBeAuthorized]
    [Compress]
    public class ComputationController : Controller
    {
        //
        // GET: /Computation/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, string returnUrl)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Redirect(returnUrl);
            }

            var mng = UserHelper.GetUserInfo(HttpContext).Computations;
            var entityId = mng.Id.GetChildId(id);
            mng.Remove(entityId);
            return Redirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Schedule(string id, string returnUrl)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Redirect(returnUrl);
            }

            var entityId = UserHelper.GetUserInfo(HttpContext).Computations.Id.GetChildId(id);
            var comp = ComputationInfo.TryLoad(entityId);
            if (comp == null) return Redirect(returnUrl);

            var state = comp.GetStatus().State;

            if (state == ComputationState.New) comp.Schedule();

            ServerManager.MasterServer.ComputationScheduler.Update();

            return Redirect(returnUrl);
        }

        public bool IsRunning(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return false;
                }

                var entityId = UserHelper.GetUserInfo(HttpContext).Computations.Id.GetChildId(id);
                var comp = ComputationInfo.TryLoad(entityId);
                if (comp == null) return false;

                return comp.IsRunning();
            }
            catch
            {
                return false;
            }
        }
        
        public ActionResult Status(string id)
        {
            var entityId = UserHelper.GetUserInfo(HttpContext).Computations.Id.GetChildId(id);
            var comp = ComputationInfo.TryLoad(entityId);
            if (comp == null) return new { }.AsJsonResult();
            return ComputationHelper.GetStatus(comp).AsJsonResult();
        }

        ////public ActionResult FullStatus(string id)
        ////{
        ////    var comp = ComputationInfo.TryLoad(EntityId.Parse(id));
        ////    if (comp == null) return new { Exists = false }.AsJsonResult();
        ////    return GetStatus(comp).AsJsonResult();
        ////}
    }
}
