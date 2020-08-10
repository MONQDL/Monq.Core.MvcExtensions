using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Monq.Tools.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="IQueryable{T}"/>.
    /// </summary>
    public static partial class QueryableExtensions
    {
        /// <summary>
        /// Включить в общий запрос <paramref name="source"/>
        /// - преобразованный <paramref name="transform"/>, по флагу <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="T">Параметр-тип запроса.</typeparam>
        /// <param name="source">Исходный запрос.</param>
        /// <param name="condition">Внешнее условие.</param>
        /// <param name="transform">Флаг для включения преобразованного запроса.</param>
        /// <returns></returns>
        public static IQueryable<T> If<T>(this IQueryable<T> source,
            bool condition,
            Func<IQueryable<T>, IQueryable<T>> transform) where T : class =>
            condition ? transform(source) : source;

        /// <summary>
        /// Включить в общий запрос <paramref name="source"/>
        /// - преобразованный <paramref name="transform"/>, по флагу <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="T">Параметр-тип запроса.</typeparam>
        /// <typeparam name="P">Параметр-тип навигационного свойства.</typeparam>
        /// <param name="source">Исходный запрос, который включает в себя выражение Include.</param>
        /// <param name="condition">Флаг для включения преобразованного запроса.</param>
        /// <param name="transform">Преобразованный запрос, который включает в себя конструкцию ThenInclude.</param>
        /// <returns></returns>
        public static IQueryable<T> If<T, P>(this IIncludableQueryable<T, P> source,
            bool condition,
            Func<IIncludableQueryable<T, P>, IQueryable<T>> transform) where T : class
            where P : class =>
            condition ? transform(source) : source;

        /// <summary>
        /// Включить в общий запрос <paramref name="source"/>
        /// - преобразованный <paramref name="transform"/>, по флагу <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="T">Параметр-тип запроса.</typeparam>
        /// <typeparam name="P">Параметр-тип навигационного свойства.</typeparam>
        /// <param name="source">Исходный запрос, который включает в себя выражение Include.</param>
        /// <param name="condition">Флаг для включения преобразованного запроса.</param>
        /// <param name="transform">Преобразованный запрос, который включает в себя конструкцию ThenInclude.</param>
        /// <returns></returns>
        public static IQueryable<T> If<T, P>(this IIncludableQueryable<T, IEnumerable<P>> source,
            bool condition,
            Func<IIncludableQueryable<T, IEnumerable<P>>, IQueryable<T>> transform)
            where T : class
            where P : class =>
            condition ? transform(source) : source;

        /// <summary>
        /// Включить в запрос выражение "<paramref name="keySelector"/> IN (...)", который бьет большой список значений на мелкие пачки,
        /// что в свою очередь дает прирост производительности в запросах.
        /// </summary>
        /// <typeparam name="TKey">Тип поля к которому будет выполнено построение предложения IN (...).</typeparam>
        /// <typeparam name="TQuery">Основная сущность в запросе.</typeparam>
        /// <param name="queryable">Запрос.</param>
        /// <param name="values">Список значение, которые будут переданы в условное выражение в пакетном режиме.</param>
        /// <param name="keySelector">Указатель поля к которому будет выполнено построение предложения IN (...).</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Размер пакета не константен, а зависит от размера поступающей коллекции.
        /// </para>
        /// https://github.com/dotnet/efcore/issues/13617
        /// <para>
        /// https://gist.github.com/kroymann/e57b3b4f30e6056a3465dbf118e5f13d
        /// </para>
        /// </remarks>
        public static IQueryable<TQuery> In<TKey, TQuery>(
            this IQueryable<TQuery> queryable,
            IEnumerable<TKey> values,
            Expression<Func<TQuery, TKey>> keySelector)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (!values.Any())
                return queryable.Take(0);

            var distinctValues = Bucketize(values);

            if (distinctValues.Length > 2048)
                throw new ArgumentException("Too many parameters for SQL Server, reduce the number of parameters",
                    nameof(keySelector));

            var predicates = distinctValues
                .Select(v =>
                {
                    // Create an expression that captures the variable so EF can turn this into a parameterized SQL query
                    Expression<Func<TKey>> valueAsExpression = () => v;
                    return Expression.Equal(keySelector.Body, valueAsExpression.Body);
                })
                .ToList();

            while (predicates.Count > 1)
                predicates = PairWise(predicates)
                    .Select(p => Expression.OrElse(p.Item1, p.Item2))
                    .ToList();

            var body = predicates.Single();

            var clause = Expression.Lambda<Func<TQuery, bool>>(body, keySelector.Parameters);

            return queryable.Where(clause);
        }

        /// <summary>
        /// Break a list of items tuples of pairs.
        /// </summary>
        static IEnumerable<(T, T)> PairWise<T>(this IEnumerable<T> source)
        {
            using var sourceEnumerator = source.GetEnumerator();
        
            while (sourceEnumerator.MoveNext())
            {
                var a = sourceEnumerator.Current;
                sourceEnumerator.MoveNext();
                var b = sourceEnumerator.Current;

                yield return (a, b);
            }
        }

        static TKey[] Bucketize<TKey>(IEnumerable<TKey> values)
        {
            var distinctValueList = values.Distinct().ToList();

            // Calculate bucket size as 1,2,4,8,16,32,64,...
            var bucket = 1;
            while (distinctValueList.Count > bucket) 
                bucket *= 2;

            // Fill all slots.
            var lastValue = distinctValueList.Last();
            
            for (var index = distinctValueList.Count; index < bucket; index++) 
                distinctValueList.Add(lastValue);

            var distinctValues = distinctValueList.ToArray();
            return distinctValues;
        }
    }
}
