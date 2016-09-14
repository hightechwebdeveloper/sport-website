using System.Configuration;
using System.Runtime.CompilerServices;
using MTDB.Core.Configuration;

namespace MTDB.Core.Infrastructure
{
    public class EngineContext
    {
        #region Methods
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static IEngine Initialize(bool forceRecreate)
        {
            if (Singleton<IEngine>.Instance == null || forceRecreate)
            {
                Singleton<IEngine>.Instance = new MtdbEngine();

                var config = ConfigurationManager.GetSection("MtdbConfig") as MtdbConfig;
                Singleton<IEngine>.Instance.Initialize(config);
            }
            return Singleton<IEngine>.Instance;
        }
        
        public static void Replace(IEngine engine)
        {
            Singleton<IEngine>.Instance = engine;
        }

        #endregion

        #region Properties
        
        public static IEngine Current
        {
            get
            {
                if (Singleton<IEngine>.Instance == null)
                {
                    Initialize(false);
                }
                return Singleton<IEngine>.Instance;
            }
        }

        #endregion
    }
}
