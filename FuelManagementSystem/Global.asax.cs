using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Mvc5;
using FuelManagementSystem.Services;
using FuelManagementSystem.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using OfficeOpenXml;

namespace FuelManagementSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var container = new UnityContainer();

            // EF DbContext
            container.RegisterType<FuelManagementSystemEntities>(new HierarchicalLifetimeManager());

            // HttpClient with CookieContainer
            container.RegisterFactory<HttpClient>(c =>
            {
                var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                return client;
            });

            // ILogger<TotalCardService>
            container.RegisterFactory<ILogger<TotalCardService>>(c =>
                new LoggerFactory().CreateLogger<TotalCardService>());

            // TotalCardService (constructor injection works automatically now)
            container.RegisterType<TotalCardService>();

            // Set Unity resolver for MVC
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            System.Diagnostics.Debug.WriteLine("Unity DI configured at " + DateTime.Now);
        }
    }
}
