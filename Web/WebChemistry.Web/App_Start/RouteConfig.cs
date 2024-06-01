using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebChemistry.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Apps",
                url: "App/{action}/{operation}",
                defaults: new { controller = "App", action = "List", operation = "Run" }
            );

            routes.MapRoute(
                name: "PatternQuery",
                url: "PatternQuery/{action}/{id}/{query}",
                defaults: new { controller = "PatternQuery", action = "Index", id = UrlParameter.Optional, query = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "PatternQueryFromMQ",
                url: "MotiveQuery/{action}/{id}/{query}",
                defaults: new { controller = "PatternQuery", action = "Index", id = UrlParameter.Optional, query = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "MotiveAtlas",
                url: "MotiveAtlas/{action}/{categoryId}/{subcategoryId}/{motiveId}",
                defaults: new 
                {
                    controller = "MotiveAtlas", 
                    action = "Index",
                    categoryId = UrlParameter.Optional,
                    subcategoryId = UrlParameter.Optional,
                    motiveId = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "MotiveValidator",
                url: "MotiveValidator/{action}/{id}",
                defaults: new
                {
                    controller = "MotiveValidator",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "ValidatorDb",
                url: "ValidatorDb/{action}/{id}/{what}",
                defaults: new
                {
                    controller = "ValidatorDb",
                    action = "Index",
                    id = UrlParameter.Optional,
                    what = UrlParameter.Optional
                }
            );

            //routes.MapRoute(
            //    name: "ValidatorDb",
            //    url: "ValidatorDb/{action}/{id}",
            //    defaults: new
            //    {
            //        controller = "ValidatorDb",
            //        action = "Index",
            //        id = UrlParameter.Optional
            //    }
            //);

            routes.MapRoute(
                name: "MotiveValidatorDBredirect",
                url: "MotiveValidatorDb/{*Data}",
                defaults: new { controller = "ValidatorDb", action = "Index" }
            );
            
            routes.MapRoute(
               name: "MotiveValidatorDbOld",
               url: "MotiveValidatorDbOld/{action}/{id}",
               defaults: new
               {
                   controller = "MotiveValidatorDbOld",
                   action = "Index",
                   id = UrlParameter.Optional
               }
           );

            routes.MapRoute(
               name: "ChargeCalculatorDetailsData",
               url: "ChargeCalculator/DetailsData/{id}/{structure}",
               defaults: new
               {
                   controller = "ChargeCalculator",
                   action = "DetailsData",
                   id = UrlParameter.Optional,
                   structure = UrlParameter.Optional
               }
           );

            routes.MapRoute(
                name: "ChargeCalculatorResult",
                url: "ChargeCalculator/Result/{id}/{structure}",
                defaults: new
                {
                    controller = "ChargeCalculator",
                    action = "Result",
                    id = UrlParameter.Optional,
                    structure = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "ChargeCalculator",
                url: "ChargeCalculator/{action}/{id}",
                defaults: new
                {
                    controller = "ChargeCalculator",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "Data",
                url: "Data/{action}/{operation}",
                defaults: new { controller = "Data", action = "Index", operation = "List" }
            );
            
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}