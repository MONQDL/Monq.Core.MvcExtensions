using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Filters
{
    /// <summary>
    /// Атрибут добавляет поддержку парсинга запросов типа /api?array=1,5,4,3,6 в массив.
    /// </summary>
    public class ArrayInputAttribute : ActionFilterAttribute
    {
        readonly string[] _parameterNames;

        /// <summary>
        /// Разделитель элементов массива.
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ArrayInputAttribute"/>.
        /// </summary>
        /// <param name="parameterName"></param>
        public ArrayInputAttribute(params string[] parameterName)
        {
            _parameterNames = parameterName;
            Separator = ",";
        }

        /// <summary>
        /// Выполнить парсинг строки с параметрами в массив.
        /// </summary>
        void ProcessArrayInput(ActionExecutingContext actionContext, string parameterName)
        {
            if (!actionContext.ActionArguments.ContainsKey(parameterName))
                return;

            var parameterDescriptor = actionContext.ActionDescriptor.Parameters.FirstOrDefault(p => p.Name == parameterName);
            if (parameterDescriptor is null || !parameterDescriptor.ParameterType.IsArray)
                return;

            var type = parameterDescriptor.ParameterType.GetElementType();
            if (type is null)
                return;

            var parameters = string.Empty;
            if (actionContext.HttpContext.Request.Query.ContainsKey(parameterName))
                parameters = actionContext.HttpContext.Request.Query[parameterName];

            if (string.IsNullOrWhiteSpace(parameters))
                return;

            var values = parameters.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TypeDescriptor.GetConverter(type).ConvertFromString)
                .ToArray();

            var typedValues = Array.CreateInstance(type, values.Length);
            values.CopyTo(typedValues, 0);
            actionContext.ActionArguments[parameterName] = typedValues;
        }

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
