using Adims.DataAccess;
using Adims.DataAccess.Repository;
using Adims.Service;
using StructureMap;
using System;
using System.Threading;

namespace Aghsat.Ioc
{
    public class SchObjectFactory
    {
        #region Fields

        private static readonly Lazy<Container> ContainerBuilder = new Lazy<Container>(DefaultContainer, LazyThreadSafetyMode.ExecutionAndPublication);

        #endregion

        #region Properties

        public static IContainer Container
        {
            get { return ContainerBuilder.Value; }
        }
        #endregion


        public static Container DefaultContainer()
        {
            return new Container(ioc =>
            {
                ioc.For<ApplicationContext>().Use(() => new ApplicationContext()).ContainerScoped();

                ioc.For<ApplicationContext>().Use(() => new ApplicationContext()).LifecycleIs<StructureMap.Pipeline.HttpContextLifecycle>();


                ioc.Scan(x =>
                {
                    x.AssemblyContainingType<IDealerService>();
                    x.TheCallingAssembly();
                    x.WithDefaultConventions();
                });

                ioc.Scan(x =>
                {
                    x.AssemblyContainingType<ICityRepository>();
                    x.TheCallingAssembly();
                    x.WithDefaultConventions();
                });

                ioc.For<IDealerService>().Use<DealerService>();
                ioc.For<ICityService>().Use<CityService>();

                ioc.Scan(scan =>
                {
                    scan.AssemblyContainingType<DealerService>();
                    scan.WithDefaultConventions();
                });

                ioc.Scan(scan =>
                {
                    scan.AssemblyContainingType<ICityRepository>();
                    scan.WithDefaultConventions();
                });



            });
        }
    }
}