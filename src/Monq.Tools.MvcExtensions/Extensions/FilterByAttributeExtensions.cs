using Monq.Tools.MvcExtensions.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.Extensions
{
    public static class FilterByAttributeExtensions
    {
        /// <summary>
        /// Позволяет выполнить фильтрацию <paramref name="records"/> по указанным полям <paramref name="filter"/> имеющих атрибут [FilteredBy].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Y"></typeparam>
        /// <param name="records">Объект фильтрации</param>
        /// <param name="filter">Модель фильтра</param>
        /// <returns></returns>
        public static IQueryable<T> FilterByAttribute<T, Y>(this IQueryable<T> records, Y filter)
        {
            var filteredProperties = filter.GetType().GetProperties().Where(x => x.GetCustomAttribute<FilteredByAttribute>() != null);

            Expression body = null;
            var param = Expression.Parameter(typeof(T), "x");

            foreach (var property in filteredProperties)
            {
                IList filterValues = property.GetValue(filter) as IList;
                if (filterValues != null && filterValues.Count != 0)
                {
                    var filteredProperty = property.GetCustomAttribute<FilteredByAttribute>().FilteredProperty;

                    var list = Expression.Constant(filterValues);

                    var propertyType = typeof(T).GetProperty(filteredProperty)?.PropertyType;
                    if (propertyType == null) throw new Exception($"Класс {typeof(T).Name} не содержит свойства {filteredProperty}.");

                    var methodInfo = typeof(List<>).MakeGenericType(new Type[] { propertyType }).GetMethod("Contains");

                    var value = Expression.Property(param, filteredProperty);

                    if (body != null)
                        body = Expression.AndAlso(body, Expression.Call(list, methodInfo, value));
                    else
                        body = Expression.Call(list, methodInfo, value);
                }
            }

            if (body == null)
                return records;

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);

            return records.Where(lambda);
        }

    }
}
