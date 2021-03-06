﻿using System;
using System.Configuration;
using System.Text;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MTDB.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching in Redis store (http://redis.io/).
    /// Mostly it'll be used when running in a web farm or Azure.
    /// But of course it can be also used on any server or environment
    /// </summary>
    public partial class RedisCacheManager : ICacheManager
    {
        #region Fields
        
        private readonly ICacheManager _perRequestCacheManager;
        private readonly string _predicat;
        private readonly string _redisCachingConnectionString;
        private ConnectionMultiplexer _muxer;
        private IDatabase _db;

        #endregion

        #region Ctor

        public RedisCacheManager(PerRequestCacheManager perRequestCacheManager, string predicat)
        {
            var redisCachingConnectionString = ConfigurationManager.AppSettings["RedisCachingConnectionString"];
            if (string.IsNullOrEmpty(redisCachingConnectionString))
                throw new Exception("Redis connection string is empty");

            this._perRequestCacheManager = perRequestCacheManager;
            this._predicat = predicat;
            this._redisCachingConnectionString = redisCachingConnectionString;
        }

        #endregion

        #region Utilities

        private void InitConnection()
        {
            if (_muxer != null)
                return;

            this._muxer = ConnectionMultiplexer.Connect(_redisCachingConnectionString);
            this._db = _muxer.GetDatabase();
        }
        
        protected virtual byte[] Serialize(object item)
        {
            var jsonString = JsonConvert.SerializeObject(item);
            return Encoding.UTF8.GetBytes(jsonString);
        }
        protected virtual T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default(T);

            var jsonString = Encoding.UTF8.GetString(serializedObject);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public virtual T Get<T>(string key)
        {
            //little performance workaround here:
            //we use "PerRequestCacheManager" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server 500 times per HTTP request (e.g. each time to load a locale or setting)
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            if (_perRequestCacheManager.IsSet(predicatedKey))
                return _perRequestCacheManager.Get<T>(predicatedKey);

            InitConnection();

            var rValue = _db.StringGet(predicatedKey);
            if (!rValue.HasValue)
                return default(T);
            var result = Deserialize<T>(rValue);

            _perRequestCacheManager.Set(predicatedKey, result, 0);
            return result;
        }

        /// <summary>
        /// Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            if (data == null)
                return;

            InitConnection();

            var entryBytes = Serialize(data);
            var expiresIn = TimeSpan.FromMinutes(cacheTime);
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;

            _db.StringSet(predicatedKey, entryBytes, expiresIn);
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public virtual bool IsSet(string key)
        {
            //little performance workaround here:
            //we use "PerRequestCacheManager" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server 500 times per HTTP request (e.g. each time to load a locale or setting)
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            if (_perRequestCacheManager.IsSet(predicatedKey))
                return true;

            InitConnection();

            return _db.KeyExists(predicatedKey);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">/key</param>
        public virtual void Remove(string key)
        {
            InitConnection();
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            _db.KeyDelete(predicatedKey);
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public virtual void RemoveByPattern(string pattern)
        {
            InitConnection();
            var predicatedKey = !pattern.StartsWith(_predicat) ? $"{_predicat}{pattern}" : pattern;
            foreach (var ep in _muxer.GetEndPoints())
            {
                var server = _muxer.GetServer(ep);
                var keys = server.Keys(pattern: "*" + predicatedKey + "*");
                foreach (var key in keys)
                    _db.KeyDelete(key);
            }
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public virtual void Clear()
        {
            InitConnection();

            foreach (var ep in _muxer.GetEndPoints())
            {
                var server = _muxer.GetServer(ep);
                //we can use the code belwo (commented)
                //but it requires administration permission - ",allowAdmin=true"
                //server.FlushDatabase();

                //that's why we simply interate through all elements now
                var keys = server.Keys();
                foreach (var key in keys)
                    _db.KeyDelete(key);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            _muxer?.Dispose();
        }

        #endregion
    }
}