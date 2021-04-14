using Monq.Models.Abstractions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="IQueryable{T}"/>.
    /// </summary>
    public static partial class QueryableExtensions
    {
        #region v1

        /// <summary>
        /// Выполнить фильтрацию в запросе <paramref name="query"/> по полю датой <paramref name="dateSelector"/>, значение которого должно входить в <paramref name="date"/>.
        /// </summary>
        /// <typeparam name="T">Тип доменной модели.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <param name="dateSelector">Селектор поля с датой.</param>
        /// <param name="date">Принимаемая модель даты из фильтра, по значению которой будет выполнена фильтрация в поле <paramref name="dateSelector"/>.</param>
        public static IQueryable<T> FilterByDate<T>(this IQueryable<T> query, Expression<Func<T, long>> dateSelector, DatePostViewModel date)
        {
            if (!date.NotEmpty())
                return query;

            var predicate = Expression.Lambda<Func<T, bool>>(dateSelector.Body.GetExpressionToFilterByDate(typeof(long), date), dateSelector.Parameters[0]);
            return query.Where(predicate);
        }

        /// <summary>
        /// Выполнить фильтрацию в запросе <paramref name="query"/> по полю датой <paramref name="dateSelector"/>, значение которого должно входить в <paramref name="date"/>.
        /// </summary>
        /// <typeparam name="T">Тип доменной модели.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <param name="dateSelector">Селектор поля с датой.</param>
        /// <param name="date">Принимаемая модель даты из фильтра, по значению которой будет выполнена фильтрация в поле <paramref name="dateSelector"/>.</param>
        /// <returns></returns>
        public static IQueryable<T> FilterByDate<T>(this IQueryable<T> query, Expression<Func<T, long?>> dateSelector, DatePostViewModel date)
        {
            if (!date.NotEmpty())
                return query;

            var predicate = Expression.Lambda<Func<T, bool>>(dateSelector.Body.NullSafeEvalWrapper().GetExpressionToFilterByDate(typeof(long?), date), dateSelector.Parameters[0]);
            return query.Where(predicate);
        }

        static bool NotEmpty(this DatePostViewModel? date)
        {
            if (date is null)
                return false;
            if (date.Equal.HasValue)
                return true;
            if (date.LessThan.HasValue)
                return true;
            if (date.LessThanOrEqual.HasValue)
                return true;
            if (date.MoreThan.HasValue)
                return true;
            if (date.MoreThanOrEqual.HasValue)
                return true;
            if (date.Range is not null)
                if (date.Range.Start != 0 || date.Range.End != int.MaxValue)
                    return true;
            return false;
        }

        static Expression GetExpressionToFilterByDate(this Expression sourceDateExpression, Type selectorType, DatePostViewModel dateFromFilter)
        {
            if (dateFromFilter.Equal.HasValue)
                return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.Equal.Value, selectorType));

            if (dateFromFilter.Range is not null)
            {
                if (dateFromFilter.Range.Start == dateFromFilter.Range.End)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.Range.Start, selectorType));

                var dates = new[] { dateFromFilter.Range.Start, dateFromFilter.Range.End };

                var (left, right) = (
                    Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            // >= x <=
            if (dateFromFilter.LessThanOrEqual.HasValue && dateFromFilter.MoreThanOrEqual.HasValue)
            {
                if (dateFromFilter.LessThanOrEqual == dateFromFilter.MoreThanOrEqual)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual, selectorType));

                var dates = new[] { dateFromFilter.LessThanOrEqual, dateFromFilter.MoreThanOrEqual };

                var (left, right) = (
                    Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            // >= x <
            if (dateFromFilter.LessThanOrEqual.HasValue && dateFromFilter.MoreThan.HasValue)
            {
                (BinaryExpression Left, BinaryExpression Right) expressionTuple;

                if (dateFromFilter.LessThanOrEqual < dateFromFilter.MoreThan)
                    expressionTuple = (
                        Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType)),
                        Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType)));
                else if (dateFromFilter.LessThanOrEqual > dateFromFilter.MoreThan)
                    expressionTuple = (
                        Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType)),
                        Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType)));
                else
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType));

                return Expression.AndAlso(expressionTuple.Left, expressionTuple.Right);
            }

            // > x <=
            if (dateFromFilter.LessThan.HasValue && dateFromFilter.MoreThanOrEqual.HasValue)
            {
                (BinaryExpression, BinaryExpression) expressionTuple;

                if (dateFromFilter.LessThan < dateFromFilter.MoreThanOrEqual)
                    expressionTuple = (
                        Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType)),
                        Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType)));
                else if (dateFromFilter.LessThan > dateFromFilter.MoreThanOrEqual)
                    expressionTuple = (
                        Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType)),
                        Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType)));
                else
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));

                return Expression.AndAlso(expressionTuple.Item1, expressionTuple.Item2);
            }

            // > x <
            if (dateFromFilter.LessThan.HasValue && dateFromFilter.MoreThan.HasValue)
            {
                if (dateFromFilter.LessThan == dateFromFilter.MoreThan)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));

                var dates = new[] { dateFromFilter.LessThan, dateFromFilter.MoreThan };

                var (left, right) = (
                    Expression.GreaterThan(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThan(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            if (dateFromFilter.LessThanOrEqual.HasValue)
                return Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType));
            if (dateFromFilter.MoreThanOrEqual.HasValue)
                return Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType));
            if (dateFromFilter.LessThan.HasValue)
                return Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));
            if (dateFromFilter.MoreThan.HasValue)
                return Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType));

            throw new InvalidOperationException("Ошибка построения выражения по данной комбинации полей входящей модели.");
        }

        #endregion

        #region v2

        /// <summary>
        /// Выполнить фильтрацию в запросе <paramref name="query"/> по полю датой <paramref name="dateSelector"/>, значение которого должно входить в <paramref name="date"/>.
        /// </summary>
        /// <typeparam name="T">Тип доменной модели.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <param name="dateSelector">Селектор поля с датой.</param>
        /// <param name="date">Принимаемая модель даты из фильтра, по значению которой будет выполнена фильтрация в поле <paramref name="dateSelector"/>.</param>
        public static IQueryable<T> FilterByDate<T>(this IQueryable<T> query, Expression<Func<T, DateTimeOffset>> dateSelector, Models.Abstractions.v2.DatePostViewModel date)
        {
            if (!date.NotEmpty())
                return query;

            var predicate = Expression.Lambda<Func<T, bool>>(dateSelector.Body.GetExpressionToFilterByDate(typeof(DateTimeOffset), date), dateSelector.Parameters[0]);
            return query.Where(predicate);
        }

        /// <summary>
        /// Выполнить фильтрацию в запросе <paramref name="query"/> по полю датой <paramref name="dateSelector"/>, значение которого должно входить в <paramref name="date"/>.
        /// </summary>
        /// <typeparam name="T">Тип доменной модели.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <param name="dateSelector">Селектор поля с датой.</param>
        /// <param name="date">Принимаемая модель даты из фильтра, по значению которой будет выполнена фильтрация в поле <paramref name="dateSelector"/>.</param>
        public static IQueryable<T> FilterByDate<T>(this IQueryable<T> query, Expression<Func<T, DateTimeOffset?>> dateSelector, Models.Abstractions.v2.DatePostViewModel date)
        {
            if (!date.NotEmpty())
                return query;

            var predicate = Expression.Lambda<Func<T, bool>>(dateSelector.Body.NullSafeEvalWrapper().GetExpressionToFilterByDate(typeof(DateTimeOffset?), date), dateSelector.Parameters[0]);
            return query.Where(predicate);
        }

        static bool NotEmpty(this Models.Abstractions.v2.DatePostViewModel? date)
        {
            if (date is null)
                return false;
            if (date.Equal.HasValue)
                return true;
            if (date.LessThan.HasValue)
                return true;
            if (date.LessThanOrEqual.HasValue)
                return true;
            if (date.MoreThan.HasValue)
                return true;
            if (date.MoreThanOrEqual.HasValue)
                return true;
            if (date.Range != null)
                if (date.Range.Start != DateTimeOffset.MinValue || date.Range.End != DateTimeOffset.MaxValue)
                    return true;
            return false;
        }

        static Expression GetExpressionToFilterByDate(this Expression sourceDateExpression, Type selectorType, Models.Abstractions.v2.DatePostViewModel dateFromFilter)
        {
            if (dateFromFilter.Equal.HasValue)
                return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.Equal.Value, selectorType));

            if (dateFromFilter.Range != null)
            {
                if (dateFromFilter.Range.Start == dateFromFilter.Range.End)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.Range.Start, selectorType));

                var dates = new[] { dateFromFilter.Range.Start, dateFromFilter.Range.End };

                var (left, right) = (
                    Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            // >= x <=
            if (dateFromFilter.LessThanOrEqual.HasValue && dateFromFilter.MoreThanOrEqual.HasValue)
            {
                if (dateFromFilter.LessThanOrEqual == dateFromFilter.MoreThanOrEqual)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual, selectorType));

                var dates = new[] { dateFromFilter.LessThanOrEqual, dateFromFilter.MoreThanOrEqual };

                var (left, right) = (
                    Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            // >= x <
            if (dateFromFilter.LessThanOrEqual.HasValue && dateFromFilter.MoreThan.HasValue)
            {
                (BinaryExpression Left, BinaryExpression Right) expressionTuple;

                if (dateFromFilter.LessThanOrEqual < dateFromFilter.MoreThan)
                    expressionTuple = (
                        Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType)),
                        Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType)));
                else if (dateFromFilter.LessThanOrEqual > dateFromFilter.MoreThan)
                    expressionTuple = (
                        Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType)),
                        Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType)));
                else
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType));

                return Expression.AndAlso(expressionTuple.Left, expressionTuple.Right);
            }

            // > x <=
            if (dateFromFilter.LessThan.HasValue && dateFromFilter.MoreThanOrEqual.HasValue)
            {
                (BinaryExpression Left, BinaryExpression Right) expressionTuple;

                if (dateFromFilter.LessThan < dateFromFilter.MoreThanOrEqual)
                    expressionTuple = (
                        Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType)),
                        Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType)));
                else if (dateFromFilter.LessThan > dateFromFilter.MoreThanOrEqual)
                    expressionTuple = (
                        Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType)),
                        Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType)));
                else
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));

                return Expression.AndAlso(expressionTuple.Left, expressionTuple.Right);
            }

            // > x <
            if (dateFromFilter.LessThan.HasValue && dateFromFilter.MoreThan.HasValue)
            {
                if (dateFromFilter.LessThan == dateFromFilter.MoreThan)
                    return Expression.Equal(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));

                var dates = new[] { dateFromFilter.LessThan, dateFromFilter.MoreThan };

                var (left, right) = (
                    Expression.GreaterThan(sourceDateExpression, Expression.Constant(dates.Min(), selectorType)),
                    Expression.LessThan(sourceDateExpression, Expression.Constant(dates.Max(), selectorType)));

                return Expression.AndAlso(left, right);
            }

            if (dateFromFilter.LessThanOrEqual.HasValue)
                return Expression.LessThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.LessThanOrEqual.Value, selectorType));
            if (dateFromFilter.MoreThanOrEqual.HasValue)
                return Expression.GreaterThanOrEqual(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThanOrEqual.Value, selectorType));
            if (dateFromFilter.LessThan.HasValue)
                return Expression.LessThan(sourceDateExpression, Expression.Constant(dateFromFilter.LessThan.Value, selectorType));
            if (dateFromFilter.MoreThan.HasValue)
                return Expression.GreaterThan(sourceDateExpression, Expression.Constant(dateFromFilter.MoreThan.Value, selectorType));

            throw new InvalidOperationException("Ошибка построения выражения по данной комбинации полей входящей модели.");
        }

        #endregion
    }
}
