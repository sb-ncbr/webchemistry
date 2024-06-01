using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebChemistry.Platform;
using WebChemistry.Platform.Users;

namespace WebChemistry.Web.Helpers
{
    public static class UserHelper
    {
        /////// <summary>
        /////// Check if the user is authenticated.
        /////// </summary>
        /////// <param name="context"></param>
        /////// <returns></returns>
        ////public static bool IsAuthenticated(HttpContextBase context)
        ////{
        ////    return context.Request.IsAuthenticated;
        ////}

        /////// <summary>
        /////// Get the current user name.
        /////// </summary>
        /////// <param name="context"></param>
        /////// <returns></returns>
        ////public static string GetCurrentUserName(HttpContextBase context)
        ////{
        ////    return context.User.Identity.Name;
        ////}

        /// <summary>
        /// Get the current webchem platform user.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static UserInfo GetUserInfo(HttpContextBase context)
        {
            return ServerHelper.Default.Users.GetOrCreateUserByName(context.User.Identity.Name);
        }

        public static string GetUserIP(HttpRequestBase req)
        {
            string ipList = req.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrWhiteSpace(ipList)) ipList = req.ServerVariables["REMOTE_ADDR"];

            string ret = "unknown";
            if (!string.IsNullOrWhiteSpace(ipList))
            {
                ret = ipList.Split(',')[0];
            }

            return ret;
        }
    }
}
