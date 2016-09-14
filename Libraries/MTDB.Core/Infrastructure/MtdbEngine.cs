using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using MTDB.Core.Infrastructure.DependencyManagement;
using MTDB.Core.Configuration;

namespace MTDB.Core.Infrastructure
{
    public class MtdbEngine : IEngine
    {
        #region Fields

        private ContainerManager _containerManager;

        #endregion

        #region Utilities
        
        protected virtual void RegisterDependencies(MtdbConfig config)
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();
            this._containerManager = new ContainerManager(container);

            //we create new instance of ContainerBuilder
            //because Build() or Update() method can only be called once on a ContainerBuilder.

            //dependencies
            var typeFinder = new WebAppTypeFinder();
            builder = new ContainerBuilder();
            builder.RegisterInstance(config).As<MtdbConfig>().SingleInstance();
            builder.RegisterInstance(this).As<IEngine>().SingleInstance();
            builder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();
            builder.Update(container);

            //register dependencies provided by other assemblies
            builder = new ContainerBuilder();
            var drTypes = typeFinder.FindClassesOfType<IDependencyRegistrar>();
            var drInstances = new List<IDependencyRegistrar>();
            foreach (var drType in drTypes)
                drInstances.Add((IDependencyRegistrar)Activator.CreateInstance(drType));
            //sort
            drInstances = drInstances.AsQueryable().OrderBy(t => t.Order).ToList();
            foreach (var dependencyRegistrar in drInstances)
                dependencyRegistrar.Register(builder, typeFinder, config);
            builder.Update(container);

            //set dependency resolver
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }

        #endregion

        #region Methods
        
        public void Initialize(MtdbConfig config)
        {
            //register dependencies
            RegisterDependencies(config);
        }
        
        public T Resolve<T>() where T : class
        {
            return ContainerManager.Resolve<T>();
        }
        
        public object Resolve(Type type)
        {
            return ContainerManager.Resolve(type);
        }
        
        public T[] ResolveAll<T>()
        {
            return ContainerManager.ResolveAll<T>();
        }

        #endregion

        #region Properties
        
        public ContainerManager ContainerManager
        {
            get { return _containerManager; }
        }

        #endregion
    }
}
