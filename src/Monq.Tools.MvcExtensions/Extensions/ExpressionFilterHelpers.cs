using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Хелпер для работы с деревьями выражений.
    /// </summary>
    public static partial class ExpressionHelpers
    {
        /// <summary>
        /// Получить выражение "<paramref name="keySelector"/> IN (...)", который бьет большой список значений на мелкие пачки,
        /// что в свою очередь дает прирост производительности в запросах.
        /// </summary>
        /// <typeparam name="TKey">Тип поля к которому будет выполнено построение предложения IN (...).</typeparam>
        /// <typeparam name="T">Основная сущность в запросе.</typeparam>
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
        public static Expression<Func<T, bool>> GetExpressionToFilterByInClause<T, TKey>(Expression<Func<T, TKey>>? keySelector, IEnumerable<TKey>? values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));

            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            if (!values.Any())
                return _ => false;

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

            var predicate = Expression.Lambda<Func<T, bool>>(body, keySelector.Parameters);

            return predicate;
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