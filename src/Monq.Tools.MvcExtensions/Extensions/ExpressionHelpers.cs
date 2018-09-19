using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Monq.Tools.MvcExtensions.Extensions
{
    public static class ExpressionHelpers
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
            if (memberExpress == null)
                throw new ArgumentException("Expression body must be a member or unary expression.");
            return memberExpress.Member.Name;
        }

        /// <summary>
        /// Получить MemberExpression выражения.
        /// </summary>
        /// <param name="expression">Выражение.</param>
        /// <returns></returns>
        public static MemberExpression GetMemberExpression(Expression expression)
        {
            if (expression is MemberExpression)
            {
                return (MemberExpression)expression;
            }
            else if (expression is LambdaExpression)
            {
                var lambdaExpression = expression as LambdaExpression;
                if (lambdaExpression.Body is MemberExpression)
                {
                    return (MemberExpression)lambdaExpression.Body;
                }
                else if (lambdaExpression.Body is UnaryExpression)
                {
                    return ((MemberExpression)((UnaryExpression)lambdaExpression.Body).Operand);
                }
            }
            return null;
        }

        /// <summary>
        /// Получить полное имя свойства.
        /// </summary>
        /// <typeparam name="T">Тип объекта, которому принадлежит свойство</typeparam>
        /// <typeparam name="TVal">Тип свойства.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
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
            return string.Join('.', props);
        }

        /// <summary>
        /// Получить выражения доступа к свойству по его полному пути.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static Expression GetPropertyExpression(this Expression expression, string path)
        {
            return path.Split('.', StringSplitOptions.RemoveEmptyEntries)
                  .Aggregate(expression, Expression.Property);
        }

        /// <summary>
        /// Получить тип свойства по его полному пути.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static Type GetPropertyType(this Type type, string path)
            => path.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Aggregate(type, (propType, name) => (propType.IsGenericType) ? propType?.GetGenericArguments()[0]?.GetProperty(name)?.PropertyType : propType?.GetProperty(name)?.PropertyType);

        /// <summary>
        /// Добавить в выражение проверки на null.
        /// </summary>
        /// <param name="expr">Выражение.</param>
        /// <param name="defaultValue">Результат по умолчанию.</param>
        /// <returns></returns>
        public static Expression NullSafeEvalWrapper(this Expression expr, Expression defaultValue)
        {
            Expression obj;
            Expression safe = expr;

            while (!IsNullSafe(expr, out obj))
            {
                var isNull = Expression.Equal(obj, Expression.Constant(null));

                safe =
                    Expression.Condition
                    (
                        isNull,
                        defaultValue,
                        safe
                    );

                expr = obj;
            }
            return safe;
        }

        static bool IsNullSafe(Expression expr, out Expression nullableObject)
        {
            nullableObject = null;

            if (expr is MemberExpression || expr is MethodCallExpression)
            {
                Expression obj;
                MemberExpression memberExpr = expr as MemberExpression;
                MethodCallExpression callExpr = expr as MethodCallExpression;

                if (memberExpr != null)
                {
                    // Static fields don't require an instance
                    FieldInfo field = memberExpr.Member as FieldInfo;
                    if (field != null && field.IsStatic)
                        return true;

                    // Static properties don't require an instance
                    PropertyInfo property = memberExpr.Member as PropertyInfo;
                    if (property != null)
                    {
                        MethodInfo getter = property.GetGetMethod();
                        if (getter != null && getter.IsStatic)
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