using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace MTDB.Core.Caching
{
    public partial class PerRequestCacheManager : ICacheManager
    {
        private readonly string _predicat;
        private readonly HttpContext _context;
        
        public PerRequestCacheManager(string predicat)
        {
            this._predicat = predicat;
            this._context = HttpContext.Current;
        }

        protected virtual IDictionary GetItems()
        {
            if (_context != null)
                return _context.Items;

            return null;
        }
        
        public virtual T Get<T>(string key)
        {
            var items = GetItems();
            if (items == null)
                return default(T);
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            return (T)items[predicatedKey];
        }
        
        public virtual void Set(string key, object data, int cacheTime)
        {
            var items = GetItems();
            if (items == null)
                return;
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            if (data != null)
            {
                if (items.Contains(predicatedKey))
                    items[predicatedKey] = data;
                else
                    items.Add(predicatedKey, data);
            }
        }

        public virtual bool IsSet(string key)
        {
            var items = GetItems();
            if (items == null)
                return false;
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            return (items[predicatedKey] != null);
        }
        
        public virtual void Remove(string key)
        {
            var items = GetItems();
            if (items == null)
                return;
            var predicatedKey = !key.StartsWith(_predicat) ? $"{_predicat}{key}" : key;
            items.Remove(predicatedKey);
        }
        
        public virtual void RemoveByPattern(string pattern)
        {
            var items = GetItems();
            if (items == null)
                return;

            var enumerator = items.GetEnumerator();
            var predicatedKey = !pattern.StartsWith(_predicat) ? $"{_predicat}{pattern}" : pattern;
            var regex = new Regex(predicatedKey, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = new List<String>();
            while (enumerator.MoveNext())
            {
                if (regex.IsMatch(enumerator.Key.ToString()))
                {
                    keysToRemove.Add(enumerator.Key.ToString());
                }
            }

            foreach (string key in keysToRemove)
            {
                items.Remove(key);
            }
        }
        
        public virtual void Clear()
        {
            var items = GetItems();
            if (items == null)
                return;

            var enumerator = items.GetEnumerator();
            var keysToRemove = new List<String>();
            while (enumerator.MoveNext())
            {
                keysToRemove.Add(enumerator.Key.ToString());
            }

            foreach (string key in keysToRemove)
            {
                items.Remove(key);
            }
        }
        
        public virtual void Dispose()
        {
        }
    }
}