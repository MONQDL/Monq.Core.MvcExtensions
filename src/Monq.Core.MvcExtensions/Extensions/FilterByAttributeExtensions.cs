using Microsoft.EntityFrameworkCore.Query.Internal;
using Monq.Core.MvcExtensions.Extensions;
using Monq.Core.MvcExtensions.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="IQueryable{T}"/> для работы с <see cref="FilteredByAttribute"/>.
    /// </summary>
    public static class FilterByAttributeExtensions
    {
        /// <summary>
        /// Позволяет выполнить фильтрацию <paramref name="records"/> по указанным полям <paramref name="filter"/>, имеющим атрибут [FilterBy].
        /// </summary>
        /// <typeparam name="T">Тип объектов фильтрации.</typeparam>
        /// <typeparam name="Y">Тип модели фильтра, полученного из запроса.</typeparam>
        /// <param name="records">Объект фильтрации.</param>
        /// <param name="filter">Модель фильтра.</param>
        public static IQueryable<T> FilterBy<T, Y>(this IQueryable<T> records, Y filter)
        {
            var availablePropsToFilter = filter.GetType().GetFilteredProperties();
            var isEntityQuery = records.Provider.GetType() == typeof(EntityQueryProvider);

            Expression? body = null;
            var param = Expression.Parameter(typeof(T), "x");
            var filterConst = Expression.Constant(filter);
            foreach (var property in availablePropsToFilter)
            {
                Func<Expression, Type, Expression> compareExpr;
                var filterPropType = property.PropertyType;
                if (filterPropType == typeof(string))
                {
                    var filterValue = property.GetValue(filter) as string;
                    if (string.IsNullOrEmpty(filterValue))
                        continue;

                    var filterParams = Expression.MakeMemberAccess(filterConst, property);
                    compareExpr = ContainsString(filterParams);
                }
                else if (filterPropType.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    var filterValues = property.GetValue(filter) as IEnumerable;
                    if (filterValues?.Any() != true)
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

                var filteredProperties = property.GetCustomAttributes<FilteredByAttribute>().Select(x => x.FilteredProperty);

                Expression? subBody = null;
                foreach (var filteredProperty in filteredProperties)
                {
                    var propertyType = typeof(T).GetPropertyType(filteredProperty);
                    if (propertyType == null) throw new Exception($"Class {typeof(T).Name} does not contain property {filteredProperty}.");

                    var propExpressions = param.GetPropertyExpression(filteredProperty, !isEntityQuery).Reverse();
                    Expression? funcExpr = null;
                    foreach (var (par, expr) in propExpressions)
                    {
                        funcExpr = funcExpr == null
                            ? compareExpr(expr, propertyType)
                            : EnumerableAny(expr, expr.Type.GenericTypeArguments[0], Expression.Lambda(funcExpr, par), !isEntityQuery);
                    }

                    subBody = subBody != null ? Expression.OrElse(subBody, funcExpr) : funcExpr;
                }

                body = body is not null ? Expression.AndAlso(body, subBody) : subBody;
            }

            if (body == null)
                return records;

            var lambda = Expression.Lambda<Func<T, bool>>(body.Decompile().ExpressionCallsToConstants(), param);

            return records.Where(lambda);
        }

        static Func<Expression, Type, Expression> ContainsEnumerable(Expression filterVal)
        {
            // Согласно https://github.com/aspnet/EntityFrameworkCore/issues/10535 требуется передавать не константное значение в запрос,
            // а переменную.
            return (propExpr, propType) => Expression.Call(typeof(Enumerable), "Contains", new[] { propType }, filterVal, propExpr);
        }

        static Expression EnumerableAny(Expression propExpr, Type propType, Expression anyExpr, bool isNullCheck = false)
        {
            var expr = Expression.Call(typeof(Enumerable), "Any", new[] { propType }, propExpr, anyExpr);
            if (isNullCheck)
                return expr.CheckNullExpr(propExpr, Expression.Constant(false));
            return expr;
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
        static bool Any(this IEnumerable? source)
        {
            if (source is null)
                return false;

            var enumerator = source.GetEnumerator();
            return enumerator.MoveNext();
        }

        /// <summary>
        /// Получить фильтруемые свойства..
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        public static IEnumerable<PropertyInfo> GetFilteredProperties(this Type filter)
        {
            return filter
                .GetProperties()
                .Where(x =>
                (x.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)) ||
                (x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                || x.PropertyType == typeof(string)
                ) && x.GetCustomAttributes<FilteredByAttribute>().Any());
        }

        /// <summary>
        /// Определяет все ли свойства объекта являются null или пустым IEnumerable.
        /// </summary>
        /// <returns>
        ///   <c>true</c> Если объект пустой; иначе, <c>false</c>.
        /// </returns>
        public static bool IsEmpty(this object? obj)
        {
            if (obj is null) return true;
            if (obj is IEnumerable enumerable)
                return !enumerable.Any();

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.GetValue(obj) is IEnumerable propEnumerable)
                {
                    if (propEnumerable.Any())
                        return false;
                }
                else if (prop.GetValue(obj) is not null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}