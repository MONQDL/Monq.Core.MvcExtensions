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
            var filteredProperties = filter.GetType().GetFilteredProperties();

            Expression body = null;
            var param = Expression.Parameter(typeof(T), "x");
            var filterConst = Expression.Constant(filter);
            foreach (var property in filteredProperties)
            {
                Func<Expression, Type, Expression> compareExpr;
                var filterPropType = property.PropertyType;
                if (filterPropType.Equals(typeof(string)))
                {
                    var filterValue = property.GetValue(filter) as string;
                    if (string.IsNullOrEmpty(filterValue))
                        continue;

                    var filterParams = Expression.MakeMemberAccess(filterConst, property);
                    compareExpr = ContainsString(filterParams);
                }
                else if (filterPropType.IsGenericType && filterPropType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var filterValues = property.GetValue(filter) as IEnumerable;
                    if (!filterValues.Any())
                        continue;

                    var filterParams = Expression.MakeMemberAccess(filterConst, property);
                    compareExpr = ContainsEnumerable(filterParams);
                }
                else
                {
                    var filterValue = property.GetValue(filter);
                    if (filterValue == null)
                        continue;
                    var filterParams = Expression.MakeMemberAccess(filterConst, property);
                    compareExpr = Equals(filterParams);
                }

                var filteredPropertys = property.GetCustomAttributes<FilteredByAttribute>().Select(x => x.FilteredProperty);

                Expression subBody = null;
                foreach (var filteredProperty in filteredPropertys)
                {
                    var propertyType = typeof(T).GetPropertyType(filteredProperty);
                    if (propertyType == null) throw new Exception($"Класс {typeof(T).Name} не содержит свойства {filteredProperty}.");

                    var propExpr = param.GetPropertyExpression(filteredProperty)
                        .NullSafeEvalWrapper(Expression.Default(propertyType));

                    var containsExpression = compareExpr(propExpr, propertyType)
                        .NullSafeEvalWrapper(Expression.Default(typeof(bool)));

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

        static Func<Expression, Type, Expression> ContainsEnumerable(Expression filterVal)
        {
            // Согласно https://github.com/aspnet/EntityFrameworkCore/issues/10535 требуется передавать не константное значение в запрос,
            // а переменную.

            return (propExpr, propType) => Expression.Call(typeof(Enumerable), "Contains", new[] { propType }, filterVal, propExpr);
        }

        static Expression EnumerableAny(Expression propExpr, Type propType, Expression anyExpr)
        {
            return Expression.Call(typeof(Enumerable), "Any", new[] { propType }, anyExpr, propExpr);
        }

        static Func<Expression, Type, Expression> Equals(Expression filterVal)
        {
            return (propExpr, propType) => Expression.Equal(propExpr, Expression.Convert(filterVal, propType));
        }

        static Func<Expression, Type, Expression> ContainsString(Expression filterVal)
        {
            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            return (propExpr, _) => Expression.Call(propExpr, method, filterVal);
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

        /// <summary>
        /// Получить фильтруемые свойства..
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        /// <returns></returns>
        public static IEnumerable<PropertyInfo> GetFilteredProperties(this Type filter)
        {
            return filter
                .GetProperties()
                .Where(x =>
                ((x.PropertyType.IsGenericType && new[] { typeof(IEnumerable<>), typeof(Nullable<>) }
                    .Contains(x.PropertyType.GetGenericTypeDefinition()))
                || x.PropertyType.Equals(typeof(string))
                ) && x.GetCustomAttributes<FilteredByAttribute>().Any());
        }

        /// <summary>
        /// Определяет все ли свойства объекта являются null или пустым IEnumerable.
        /// </summary>
        /// <returns>
        ///   <c>true</c> Если объект пустой; иначе, <c>false</c>.
        /// </returns>
        public static bool IsEmpty(this object obj)
        {
            if (obj == null) return true;
            if (obj is IEnumerable enumerable)
                return !enumerable.Any();

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.GetValue(obj) is IEnumerable propEnumerable)
                {
                    if (propEnumerable.Any())

                        return false;
                }
                else if (prop.GetValue(obj) != null)
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetFullPropertyName<T>(Expression<Func<T, object>> expr)
            => ExpressionHelpers.GetFullPropertyName<T, object>(expr);
    }
}