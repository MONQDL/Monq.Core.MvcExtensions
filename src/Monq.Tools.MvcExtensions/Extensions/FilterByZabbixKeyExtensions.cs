using Monq.Tools.MvcExtensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Monq.Tools.MvcExtensions.Extensions
{
    public static class FilterByZabbixKeyExtensions
    {
        /// <summary>
        /// Отфильтровать по ZabbixKey.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src">The source.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="zabbixId">The zabbix identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        public static IQueryable<T> FilterByZabbixKey<T>(this IQueryable<T> src, IEnumerable<ZabbixKey> keys, Expression<Func<T, int>> zabbixId, Expression<Func<T, long>> elementId)
        {
            if (keys?.Any() != true)
                return src;

            Expression body = null;
            var param = Expression.Parameter(typeof(T), "x");

            var zabbixIdPropertyName = zabbixId.GetPropertyName();
            var elementIdPropertyName = elementId.GetPropertyName();

            foreach (var key in keys.Where(x => x != null).GroupBy(x => x.ZabbixId))
            {
                var zabbixIdConst = Expression.Constant(key.Key);
                var zabbixIdProp = Expression.Property(param, zabbixIdPropertyName);

                var elementIdProp = Expression.Property(param, elementIdPropertyName);

                var zabbixIdEqualExp = Expression.Equal(zabbixIdConst, zabbixIdProp);
                var containsExp = Expression.Call(typeof(Enumerable), "Contains", new[] { typeof(long) }, Expression.Constant(key.Select(x => x.ElementId)), elementIdProp);
                var keyExpr = Expression.AndAlso(zabbixIdEqualExp, containsExp);

                if (body != null)
                    body = Expression.OrElse(body, keyExpr);
                else
                    body = keyExpr;
            }

            if (body == null)
                return src;

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);
            return src.Where(lambda);
        }
    }
}