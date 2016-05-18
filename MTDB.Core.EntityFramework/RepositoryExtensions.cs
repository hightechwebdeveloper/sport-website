using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MTDB.Core.EntityFramework.Entities;

namespace MTDB.Core.EntityFramework
{
    public static class RepositoryExtensions
    {

        public static IQueryable<Player> WithStats(this IQueryable<Player> player)
        {
            return
                player.Include(p => p.Stats.Select(y => y.Stat.Category))
                    .Include(p => p.PrimaryPosition)
                    .Include(p => p.SecondaryPosition);
        }

        public static IQueryable<Lineup> IncludePlayer<TProperty>(this IQueryable<Lineup> lineups,
            Func<Lineup, TProperty> player) where TProperty : Player
        {
            return lineups
                .Include(l => player(l))
                .Include(l => player(l).Stats.Select(y => y.Stat.Category));
        }

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

        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            // note: creating a Random instance each call may not be correct for you,
            // consider a thread-safe static instance
            var r = new Random();
            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[r.Next(0, list.Count)];
        }

        public static IEnumerable<MTDBPackLeaderboard> GetMTDBLeaderboard(this MtdbRepository repository, int count, string packType, DateTimeOffset? startDate)
        {
            var query = $"SELECT TOP({count}) * FROM vw_Leaderboard WHERE 1=1 ";
            if (!string.IsNullOrWhiteSpace(packType))
            {
                query = query + $"AND Pack = '{packType}' ";
            }
            if (startDate.HasValue)
            {
                query = query + $"AND DateTime >= '{startDate}' ";
            }

            query = query + "ORDER BY Score DESC";

            var values =
                repository.Database.SqlQuery<MTDBPackLeaderboard>(query);
            return values;
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


        public class MTDBPackLeaderboard
        {
            public string Name { get; set; }
            public string User { get; set; }
            public DateTimeOffset DateTime { get; set; }
            public string Pack { get; set; }
            public int Score { get; set; }
            public int Id { get; set; }
        }
    }
}
