using Microsoft.AspNetCore.Mvc.Filters;
using Monq.Core.MvcExtensions.Helpers;
using System;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Filters
{
    /// <summary>
    /// Атрибут добавляет поддержку парсинга запросов типа /api?array=1,5,4,3,6 в массив.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ArrayInputAttribute : ActionFilterAttribute
    {
        readonly string[] _parameterNames;

        /// <summary>
        /// An array string separator.
        /// </summary>
        public string Separator { get; set; } = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayInputAttribute"/>.
        /// </summary>
        public ArrayInputAttribute(params string[] parameterName)
            => _parameterNames = parameterName;

        /// <summary>
        /// Convert Action Argument string into a string array.
        /// </summary>
        void ProcessArrayInput(ActionExecutingContext actionContext, string parameterName)
            => ActionFilterAttributeHelper.ProcessArrayInput(actionContext, parameterName, Separator);

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var param in _parameterNames)
                ProcessArrayInput(context, param);
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var param in _parameterNames)
                ProcessArrayInput(context, param);

            await next();
        }
    }
}
