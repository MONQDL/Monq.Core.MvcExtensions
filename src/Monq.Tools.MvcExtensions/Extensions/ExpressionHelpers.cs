using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        static MemberExpression GetMemberExpression<T, TVal>(Expression<Func<T, TVal>> expression)
        {
            var member = expression.Body as MemberExpression;
            var unary = expression.Body as UnaryExpression;
            return member ?? (unary != null ? unary.Operand as MemberExpression : null);
        }
    }
}