using System;
using System.Linq.Expressions;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Преобразует вызываемые методы, которые EF не может преобразововать, в константы
    /// </summary>
    /// <seealso cref="ExpressionVisitor" />
    public class ExpressionConstantCallVisitor : ExpressionVisitor
    {
        public static Expression ExpressionCallsToConstants(Expression expression)
        {
            return new ExpressionConstantCallVisitor().Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Object?.Type == typeof(DateTimeOffset))
            {
                return Expression.Constant(Expression.Lambda(Expression.Call(node.Object, node.Method)).Compile().DynamicInvoke());
            }
            return base.VisitMethodCall(node);
        }
    }
}