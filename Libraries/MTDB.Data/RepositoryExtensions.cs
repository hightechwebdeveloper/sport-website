using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MTDB.Data.Entities;

namespace MTDB.Data
{
    public static class RepositoryExtensions
    {
        public static IQueryable<T> FilterByCreatedDate<T>(this IQueryable<T> query, DateTimeOffset date) where T : EntityBase
        {
            return query.Where(p => p.CreatedDate.Year == date.Year)
                .Where(p => p.CreatedDate.Month == date.Month)
                .Where(p => p.CreatedDate.Day == date.Day);
        }
       
        public static T AttachById<T>(this DbSet<T> dbSet, int? id) where T : EntityBase, new()
        {
            if (!id.HasValue)
                return null;

            T obj = dbSet.Local.FirstOrDefault(x => x.Id == id);

            if (obj == null)
            {
                obj = new T { Id = id.Value };
                dbSet.Attach(obj);
            }

            return obj;
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> source, string name, SortOrder sortOrder, string defaultColumn, int skip, int take, Dictionary<string, string> nameMap = null)
        {
            if (nameMap != null && nameMap.Any())
            {
                // Make sure we don't care about case.
                if (!Equals(nameMap.Comparer, StringComparer.InvariantCultureIgnoreCase))
                {
                    nameMap = new Dictionary<string, string>(nameMap, StringComparer.InvariantCultureIgnoreCase);
                }

                if (nameMap.ContainsKey(name))
                {
                    name = nameMap[name];
                }
            }

            if (sortOrder == SortOrder.Unspecified)
            {
                sortOrder = SortOrder.Descending;
            }

            if (skip < 0)
            {
                skip = 0;
            }

            if (take > 100)
            {
                take = 100;
            }

            if (sortOrder == SortOrder.Ascending)
            {
                try
                {
                    return source.OrderBy(name).Skip(skip).Take(take);
                }
                catch
                {
                    return source.OrderBy(defaultColumn).Skip(skip).Take(take);
                }

            }

            try
            {
                return source.OrderByDescending(name).Skip(skip).Take(take);
            }
            catch
            {
                return source.OrderByDescending(defaultColumn).Skip(skip).Take(take);
            }
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> source, string name, SortOrder sortOrder, string defaultColumn, Dictionary<string, string> nameMap = null)
        {
            if (nameMap != null && nameMap.Any())
            {
                if (!Equals(nameMap.Comparer, StringComparer.InvariantCultureIgnoreCase))
                    nameMap = new Dictionary<string, string>(nameMap, StringComparer.InvariantCultureIgnoreCase);

                if (nameMap.ContainsKey(name))
                    name = nameMap[name];
            }

            if (sortOrder == SortOrder.Unspecified)
                sortOrder = SortOrder.Descending;

            return sortOrder == SortOrder.Ascending 
                ? source.OrderBy(defaultColumn) 
                : source.OrderByDescending(name);
        }

        private static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = typeof(T);
            var property = type.GetNestedProperty(ordering);
            var orderByExp = CreateExpression(type, ordering);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderBy", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        private static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = typeof(T);
            var property = type.GetNestedProperty(ordering);
            var orderByExp = CreateExpression(type, ordering);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderByDescending", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }
        
        public static PropertyInfo GetNestedProperty(this Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null && name.Contains("."))
            {
                var split = name.Split('.');
                var firstName = split[0];
                var nestedName = split[1];
                var nested = type.GetProperty(firstName);
                property = nested.PropertyType.GetProperty(nestedName);
            }

            return property;
        }

        public static LambdaExpression CreateExpression(Type type, string propertyName)
        {
            var param = Expression.Parameter(type, "x");
            Expression body = param;
            foreach (var member in propertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body, member);
            }
            return Expression.Lambda(body, param);
        }
    }
}
