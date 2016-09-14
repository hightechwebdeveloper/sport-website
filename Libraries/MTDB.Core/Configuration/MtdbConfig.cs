using System;
using System.Configuration;
using System.Xml;

namespace MTDB.Core.Configuration
{
    public partial class MtdbConfig : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            var config = new MtdbConfig();

            var redisCachingNode = section.SelectSingleNode("RedisCaching");
            if (redisCachingNode != null && redisCachingNode.Attributes != null)
            {
                var enabledAttribute = redisCachingNode.Attributes["Enabled"];
                if (enabledAttribute != null)
                    config.RedisCachingEnabled = Convert.ToBoolean(enabledAttribute.Value);

                var connectionStringAttribute = redisCachingNode.Attributes["ConnectionString"];
                if (connectionStringAttribute != null)
                    config.RedisCachingConnectionString = connectionStringAttribute.Value;
            }

            return config;
        }

        /// <summary>
        /// Indicates whether we should use Redis server for caching (instead of default in-memory caching)
        /// </summary>
        public bool RedisCachingEnabled { get; private set; }
        /// <summary>
        /// Redis connection string. Used when Redis caching is enabled
        /// </summary>
        public string RedisCachingConnectionString { get; private set; }
    }
}
