using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Monq.Core.MvcExtensions.Extensions;

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
    /// <typeparam name="T">Основная сущность в запросе.</typeparam>
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
    public static IQueryable<T> In<TKey, T>(
        this IQueryable<T> queryable,
        IEnumerable<TKey> values,
        Expression<Func<T, TKey>> keySelector)
    {
        var clause = ExpressionHelpers.GetExpressionToFilterByInClause(keySelector, values);
        return queryable.Where(clause);
    }

    /// <summary>
    /// Select properties only by paths <paramref name="propertyPaths"/>.
    /// </summary>
    /// <param name="source">Query.</param>
    /// <param name="propertyPaths">Paths to properties in type <typeparamref name="T"/>.</param>
    /// <typeparam name="T">Query type param.</typeparam>
    [RequiresUnreferencedCode("SelectProperties uses reflection to get T properties and is incompatible with trimming.")]
    public static IQueryable<T> SelectProperties<T>(this IQueryable<T>? source, IEnumerable<string>? propertyPaths)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (propertyPaths.CollectionIsNullOrEmpty())
            return source;

        var queryType = typeof(T);
        var lambdaParameter = Expression.Parameter(queryType);

        // Nested properties are not supported, select only first level properties.
        var propertyNames = propertyPaths.Select(x => x.Split(".")[0]).Distinct();

        var bindings = propertyNames
            .Select(propertyName => Expression.Property(lambdaParameter, propertyName))
            .Select(member => Expression.Bind(member.Member, member));
        var lambdaBody = Expression.MemberInit(Expression.New(queryType), bindings);
        var selector = Expression.Lambda<Func<T, T>>(lambdaBody, lambdaParameter);

        return source.Select(selector);
    }
}
