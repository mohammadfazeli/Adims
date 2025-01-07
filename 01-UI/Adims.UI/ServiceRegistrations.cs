using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Adims.DataAccess;
using Adims.DataAccess.Repository;
using Adims.Service;
using StructureMap;
using StructureMap.Pipeline;

namespace Adims.UI
{

    public class StructureMapRegistry : Registry
    {
        public StructureMapRegistry()
        {
            // Register dependencies
            For<IDealerService>().Use<DealerService>();
            For<ICityService>().Use<CityService>();

            Scan(scan =>
            {
                scan.AssemblyContainingType<DealerService>();
                scan.WithDefaultConventions();
            });

            Scan(scan =>
            {
                scan.AssemblyContainingType<CityRepository>();
                scan.WithDefaultConventions();
            });

            For<ApplicationContext>().Use(() => new ApplicationContext()).ContainerScoped();
        }
    }

    public static class IoC
    {
        public static IContainer Initialize()
        {
            return new Container(c => c.AddRegistry<StructureMapRegistry>());
        }
    }
}