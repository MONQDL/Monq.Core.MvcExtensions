using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DelegateDecompiler;
using Monq.Core.MvcExtensions.Extensions;

namespace Monq.Tools.MvcExtensions.Extensions
{
    /// <summary>
    /// Хелпер для работы с деревьями выражений.
    /// </summary>
    public static partial class ExpressionHelpers
    {
        /// <summary>
        /// Получить название свойства из лямбда-выражения.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Expression body must be a member expression</exception>
        public static string GetPropertyName<T, TVal>(this Expression<Func<T, TVal>> expression)
        {
            var memberExpress = GetMemberExpression(expression);
            if (memberExpress is null)
                throw new ArgumentException("Expression body must be a member or unary expression.");
            return memberExpress.Member.Name;
        }

        /// <summary>
        /// Получить MemberExpression выражения.
        /// </summary>
        /// <param name="expression">Выражение.</param>
        public static MemberExpression? GetMemberExpression(Expression? expression) =>
            expression switch
            {
                MemberExpression memberExpression => memberExpression,
                LambdaExpression lambdaExpression when lambdaExpression.Body is MemberExpression body => body,
                LambdaExpression lambdaExpression when lambdaExpression.Body is UnaryExpression unaryExpression =>
                    (MemberExpression) unaryExpression.Operand,
                _ => null
            };

        /// <summary>
        /// Получить полное имя свойства.
        /// </summary>
        /// <typeparam name="T">Тип объекта, которому принадлежит свойство</typeparam>
        /// <typeparam name="TVal">Тип свойства.</typeparam>
        /// <param name="expression">The expression.</param>
        public static string GetFullPropertyName<T, TVal>(this Expression<Func<T, TVal>> expression)
        {
            var props = new List<string>();
            var member = GetMemberExpression(expression);
            while (member != null)
            {
                props.Add(member.Member.Name);
                member = GetMemberExpression(member.Expression);
            }
            props.Reverse();
            return string.Join(".", props);
        }

        /// <summary>
        /// Получить выражения доступа к свойству по его полному пути.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="path">The path.</param>
        public static IEnumerable<(ParameterExpression Par, Expression Expr)> GetPropertyExpression(this Expression expression, string path, bool IsNullSafe = true)
        {
            var par = (ParameterExpression)expression;
            var expr = expression;
            foreach (var propName in path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (expr.Type.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    var type = expr.Type.GenericTypeArguments[0];
                    par = Expression.Parameter(type, "par" + propName);
                    yield return (par, (!IsNullSafe) ? expr : expr.NullSafeEvalWrapper());
                    expr = par;
                }
                expr = Expression.Property(expr, propName);
            }
            yield return (par, (!IsNullSafe) ? expr : expr.NullSafeEvalWrapper());
        }

        /// <summary>
        /// Получить значение по умолчанию.
        /// </summary>
        /// <param name="type">The type.</param>
        public static object? GetDefault(this Type type) =>
            type.IsValueType ? Activator.CreateInstance(type) : null;

        public static Expression GetDefaultConstantExpr(this Expression expr) =>
            Expression.Constant(expr.Type.GetDefault(), expr.Type);

        /// <summary>
        /// Обернуть выражение в проверку на null.
        /// </summary>
        /// <param name="expr">Выражение которое необходимо выполнить.</param>
        /// <param name="val">Выражение которое необходимо сравнить с null.</param>
        /// <param name="defaultValue">Значение по умолчанию, которое должно вернуться, если null.</param>
        public static Expression CheckNullExpr(this Expression expr, Expression val, Expression defaultValue)
                => Expression.Condition(Expression.Equal(val, Expression.Constant(null, val.Type)), defaultValue, expr);

        /// <summary>
        /// Получить тип свойства по его полному пути.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        public static Type? GetPropertyType(this Type? type, string path)
            => path.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)
                .Aggregate(type,
                    (propType, name) => propType.IsGenericType
                        ? propType?.GetGenericArguments()[0].GetProperty(name)?.PropertyType
                        : propType?.GetProperty(name)?.PropertyType);

        /// <summary>
        /// Добавить в выражение проверки на null.
        /// </summary>
        /// <param name="expr">Выражение.</param>
        /// <param name="defaultValue">Результат по умолчанию.</param>
        /// <returns></returns>
        public static Expression NullSafeEvalWrapper(this Expression expr, Expression defaultValue)
        {
            var safe = expr;

            while (!IsNullSafe(expr, out Expression obj))
            {
                safe = safe.CheckNullExpr(obj, defaultValue);
                expr = obj;
            }
            return safe;
        }

        /// <summary>
        /// Декомпилировать свойства помеченные атрибутом Computed.
        /// </summary>
        /// <param name="expr">The expr.</param>
        /// <returns></returns>
        public static Expression Decompile(this Expression expr)
            => DecompileExpressionVisitor.Decompile(expr);

        /// <summary>
        /// Превратить вызовы в константы (на данный момент поддерживается только вызовы над DateTimeOffsets).
        /// </summary>
        /// <param name="expr">The expr.</param>
        /// <returns></returns>
        public static Expression ExpressionCallsToConstants(this Expression expr)
            => ExpressionConstantCallVisitor.ExpressionCallsToConstants(expr);

        /// <summary>
        /// Добавить в выражение проверки на null.
        /// </summary>
        /// <param name="expr">Выражение.</param>
        public static Expression NullSafeEvalWrapper(this Expression expr) =>
            expr.NullSafeEvalWrapper(expr.GetDefaultConstantExpr());

        static bool IsNullSafe(Expression? expr, out Expression? nullableObject)
        {
            nullableObject = null;

            if (expr is MemberExpression || expr is MethodCallExpression)
            {
                Expression? obj;
                var callExpr = expr as MethodCallExpression;

                if (expr is MemberExpression memberExpr)
                {
                    // Static fields don't require an instance
                    if (memberExpr.Member is FieldInfo field && field.IsStatic)
                        return true;

                    // Static properties don't require an instance
                    if (memberExpr.Member is PropertyInfo property)
                    {
                        var getter = property.GetGetMethod();
                        if (getter is not null && getter.IsStatic)
                            return true;
                    }
                    obj = memberExpr.Expression;
                }
                else
                {
                    // Static methods don't require an instance
                    if (callExpr.Method.IsStatic)
                        return true;

                    obj = callExpr.Object;
                }

                // Value types can't be null
                if (obj.Type.IsValueType)
                    return true;

                // Instance member access or instance method call is not safe
                nullableObject = obj;
                return false;
            }
            return true;
        }
    }
}