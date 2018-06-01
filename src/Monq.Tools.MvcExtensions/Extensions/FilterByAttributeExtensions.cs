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
                .Where(x =>
                ((x.PropertyType.IsGenericType && new[] { typeof(IEnumerable<>), typeof(Nullable<>) }
                    .Contains(x.PropertyType.GetGenericTypeDefinition()))
                || x.PropertyType.Equals(typeof(string))
                ) && x.GetCustomAttributes<FilteredByAttribute>().Any());

            Expression body = null;
            var param = Expression.Parameter(typeof(T), "x");

            foreach (var property in filteredProperties)
            {
                Func<Expression, Type, Expression> compareExpr;

                if (property.PropertyType.Equals(typeof(string)))
                {
                    var filterValue = property.GetValue(filter) as string;
                    if (string.IsNullOrEmpty(filterValue))
                        continue;

                    compareExpr = ContainsString(filterValue);
                }
                else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var filterValues = property.GetValue(filter) as IEnumerable;
                    if (!filterValues.Any())
                        continue;
                    compareExpr = ContainsEnumerable(filterValues);
                }
                else
                {
                    var filterValue = property.GetValue(filter);
                    if (filterValue == null)
                        continue;
                    var constExpr = Expression.Constant(filterValue);
                    compareExpr = Equals(constExpr);
                }

                var filteredPropertys = property.GetCustomAttributes<FilteredByAttribute>().Select(x => x.FilteredProperty);

                Expression subBody = null;
                foreach (var filteredProperty in filteredPropertys)
                {
                    var propertyType = typeof(T).GetProperty(filteredProperty)?.PropertyType;
                    if (propertyType == null) throw new Exception($"Класс {typeof(T).Name} не содержит свойства {filteredProperty}.");

                    var propExpr = Expression.Property(param, filteredProperty);
                    var containsExpression = compareExpr(propExpr, propertyType);

                    if (subBody != null)
                        subBody = Expression.OrElse(subBody, containsExpression);
                    else
                        subBody = containsExpression;
                }
                if (body != null)
                    body = Expression.AndAlso(body, subBody);
                else
                    body = subBody;
            }

            if (body == null)
                return records;

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);

            return records.Where(lambda);
        }

        static Func<Expression, Type, Expression> ContainsEnumerable(IEnumerable filter)
        {
            var constExpr = Expression.Constant(filter);
            return (propExpr, propType) => Expression.Call(typeof(Enumerable), "Contains", new[] { propType }, constExpr, propExpr);
        }

        static Func<Expression, Type, Expression> Equals(Expression constExpr)
        {
            return (propExpr, propType) => Expression.Equal(propExpr, constExpr);
        }

        static Func<Expression, Type, Expression> ContainsString(string filter)
        {
            var constExpr = Expression.Constant(filter);
            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            return (propExpr, _) => Expression.Call(propExpr, method, constExpr);
        }

        /// <summary>
        /// Реализация метода расширения Any() для перечислителя неуниверсальной коллекции.
        /// </summary>
        /// <param name="source">Неуниверсальная коллекция.</param>
        /// <returns>Истина, если в коллекции есть хотя бы 1 элемент.</returns>
        static bool Any(this IEnumerable source)
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