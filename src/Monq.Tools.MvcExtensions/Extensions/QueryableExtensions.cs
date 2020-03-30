using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;

namespace Monq.Tools.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class QueryableExtensions
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
    }
}
