using System;
using System.Collections.Generic;
using System.Linq;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для работы с коллекциями типа <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Преобразовать коллекцию <paramref name="source"/> в преобразованную <paramref name="transform"/>,
        /// по флагу <paramref name="condition"/>.
        /// </summary>
        /// <typeparam name="T">Параметр-тип запроса.</typeparam>
        /// <param name="source">Исходный запрос.</param>
        /// <param name="condition">Внешнее условие.</param>
        /// <param name="transform">Флаг для включения преобразованного запроса.</param>
        public static IEnumerable<T> If<T>(this IEnumerable<T> source,
            bool condition,
            Func<IEnumerable<T>, IEnumerable<T>> transform) where T : class =>
            condition ? transform(source) : source;

#if NET6_0
#else
        /// <summary>
        /// Выполнить фильтрацию по уникальным значениям свойства модели.
        /// </summary>
        /// <typeparam name="T">Тип, из которого состоят элементы коллекции.</typeparam>
        /// <typeparam name="TKey">Ключ фильтрации.</typeparam>
        /// <param name="source">Коллекция, в которой будет произведена фильтрация по уникальным значениям свойства модели.</param>
        /// <param name="keySelector">Выражение, содержащее ключ фильтрации.</param>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (T element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
        }
#endif

        /// <summary>
        /// Объединить две коллекции в кортеж по заданному ключу.
        /// </summary>
        /// <typeparam name="TX">Тип элемента первой коллекции.</typeparam>
        /// <typeparam name="TY">Тип элемента второй коллекции.</typeparam>
        /// <typeparam name="TSelector">Тип общего ключа для объединения коллекций.</typeparam>
        /// <param name="firstSource">Первая коллекция.</param>
        /// <param name="secondSource">Вторая коллекция.</param>
        /// <param name="firstSourceKeySelector">Селектор ключа первой коллекции.</param>
        /// <param name="secondSourceKeySelector">Селектор ключа второй коллекции.</param>
        public static IEnumerable<(TX, TY)> MergeIntoCortege<TX, TY, TSelector>(
            this IEnumerable<TX> firstSource,
            IEnumerable<TY> secondSource,
            Func<TX, TSelector> firstSourceKeySelector,
            Func<TY, TSelector> secondSourceKeySelector)
            where TX : class
            where TY : class =>
            firstSource.Join(secondSource, firstSourceKeySelector, secondSourceKeySelector, (s, v) => (s, v)).ToList();

        /// <summary>
        /// Проверить не пустая ли коллекция.
        /// </summary>
        public static bool IsAny<T>(this IEnumerable<T>? data) => data is not null && data.Any();

        /// <summary>
        /// Содержит ли коллекция всего 1 элемент.
        /// </summary>
        /// <param name="source">Коллекция.</param>
        /// <param name="value">Единственный элемент.</param>
        public static bool HasSingle<T>(this IEnumerable<T>? source, out T? value)
        {
            if (source is null)
            {
                value = default;
                return false;
            }

            if (source is IList<T> list)
            {
                if (list.Count == 1)
                {
                    value = list[0];
                    return true;
                }
            }
            else
            {
                using var iter = source.GetEnumerator();
                if (iter.MoveNext())
                {
                    value = iter.Current;
                    if (!iter.MoveNext())
                        return true;
                }
            }

            value = default;
            return false;
        }
    }
}
