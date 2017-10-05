using Monq.Tools.MvcExtensions.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Monq.Tools.MvcExtensions.Extensions
{
    public static class FilterByAttributeExtensions
    {
        /// <summary>
        /// Позволяет выполнить фильтрацию <paramref name="records"/> по указанным полям <paramref name="filter"/>, имеющим атрибут [FilterBy].
        /// </summary>
        /// <typeparam name="T">Тип объектов фильтрации.</typeparam>
        /// <typeparam name="Y">Тип модели фильтра, полученного из запроса.</typeparam>
        /// <param name="records">Объект фильтрации.</param>
        /// <param name="filter">Модель фильтра.</param>
        /// <returns></returns>
        public static IQueryable<T> FilterBy<T, Y>(this IQueryable<T> records, Y filter)
        {
            var filteredProperties = filter
                .GetType()
                .GetProperties()
                .Where(x => x.GetCustomAttribute<FilteredByAttribute>() != null);

            Expression body = null;
            var param = Expression.Parameter(typeof(T), "x");

            foreach (var property in filteredProperties)
            {
                var filterValues = property.GetValue(filter) as IEnumerable;
                if (!filterValues.Any())
                    continue;

                var filteredProperty = property.GetCustomAttribute<FilteredByAttribute>().FilteredProperty;
                var list = Expression.Constant(filterValues);

                var propertyType = typeof(T).GetProperty(filteredProperty)?.PropertyType;
                if (propertyType == null) throw new Exception($"Класс {typeof(T).Name} не содержит свойства {filteredProperty}.");

                var methodInfo = typeof(IEnumerable<>).MakeGenericType(new Type[] { propertyType }).GetMethod("Contains");

                var value = Expression.Property(param, filteredProperty);

                if (body != null)
                    body = Expression.AndAlso(body, Expression.Call(list, methodInfo, value));
                else
                    body = Expression.Call(list, methodInfo, value);
            }

            if (body == null)
                return records;

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);

            return records.Where(lambda);
        }

        /// <summary>
        /// Реализация метода расширения Any() для перечислителя неуниверсальной коллекции.
        /// </summary>
        /// <param name="source">Неуниверсальная коллекция.</param>
        /// <returns>Истина, если в коллекции есть хотя бы 1 элемент.</returns>
        public static bool Any(this IEnumerable source)
        {
            if (source == null)
                return false;

            var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext())
                return true;

            return false;
        }
    }
}
