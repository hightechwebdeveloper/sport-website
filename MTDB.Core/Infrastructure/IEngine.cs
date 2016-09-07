using System;
using MTDB.Core.Infrastructure.DependencyManagement;
using MTDB.Core.Configuration;

namespace MTDB.Core.Infrastructure
{
    public interface IEngine
    {
        ContainerManager ContainerManager { get; }
        
        void Initialize(MtdbConfig config);
        
        T Resolve<T>() where T : class;
        
        object Resolve(Type type);
        
        T[] ResolveAll<T>();
    }
}
