using StructureMap;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Adims.UI
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            UnityConfig.RegisterComponents(); // Initialize Unity

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //var container = new Container(new IoCRegistry());
            //// Set MVC Dependency Resolver
            //DependencyResolver.SetResolver(new StructureMapDependencyResolver(container));

        }
    }

    //public class StructureMapControllerFactory : DefaultControllerFactory
    //{
    //    protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
    //    {

    //        if (controllerType == null)
    //        {
    //            throw new HttpException(404, $"Resource not found : {requestContext.HttpContext.Request.Path}");
    //        }
    //        return SchObjectFactory.Container.GetInstance(controllerType) as Controller;
    //    }
    //}
}
