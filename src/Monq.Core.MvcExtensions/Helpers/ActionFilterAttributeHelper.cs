using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.ComponentModel;
using System.Linq;

namespace Monq.Core.MvcExtensions.Helpers
{
    /// <summary>
    /// Helper class to work with ActionFilterAttribute.
    /// </summary>
    public static class ActionFilterAttributeHelper
    {
        /// <summary>
        /// Convert Action Argument string into a string array.
        /// </summary>
        public static void ProcessArrayInput(ActionExecutingContext context, string parameterName, string separator)
        {
            if (!context.ActionArguments.ContainsKey(parameterName))
                return;

            context.ActionArguments[parameterName] = null;
            var parameterDescriptor = context.ActionDescriptor.Parameters.FirstOrDefault(p => p.Name == parameterName);
            if (parameterDescriptor is null || !parameterDescriptor.ParameterType.IsArray)
                return;

            var type = parameterDescriptor.ParameterType.GetElementType();
            if (type is null)
                return;

            var parameters = string.Empty;
            if (context.HttpContext.Request.Query.ContainsKey(parameterName))
                parameters = context.HttpContext.Request.Query[parameterName];

            if (string.IsNullOrWhiteSpace(parameters))
                return;

            var values = parameters.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TypeDescriptor.GetConverter(type).ConvertFromString)
                .ToArray();

            var typedValues = Array.CreateInstance(type, values.Length);
            values.CopyTo(typedValues, 0);
            context.ActionArguments[parameterName] = typedValues;
        }
    }
}
