using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monq.Core.MvcExtensions.Helpers;
using Monq.Core.MvcExtensions.JsonContractResolvers;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Filters
{
    /// <summary>
    /// Apply gRpc.FieldMask to the ActionResult.
    /// </summary>
    public class FieldMaskFilterAttribute : ActionFilterAttribute
    {
        const string _fieldMaskParameterName = "fieldMask";
        readonly HashSet<string> _parameterNames;
        readonly List<string> _propNamesToSerialize;

        /// <summary>
        /// An array string separator.
        /// </summary>
        public string Separator { get; set; } = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldMaskFilterAttribute"/> class.
        /// </summary>
        public FieldMaskFilterAttribute(params string[] parameterName)
        {
            if (parameterName is null || !parameterName.Any())
                _parameterNames = new HashSet<string> { _fieldMaskParameterName };
            else
                _parameterNames = new HashSet<string>(parameterName);

            _propNamesToSerialize = new List<string>();
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _propNamesToSerialize.Clear();

            foreach (var param in _parameterNames)
                ProcessArrayInput(context, param);
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _propNamesToSerialize.Clear();

            foreach (var param in _parameterNames)
                ProcessArrayInput(context, param);

            await next();
        }
        
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

            if (typedValues is string[] propNames)
                _propNamesToSerialize.AddRange(propNames);

            actionContext.ActionArguments[parameterName] = typedValues;
        }

        /// <inheritdoc />
        public override void OnResultExecuting(ResultExecutingContext context)
            => ProcessResult(context);

        /// <inheritdoc />
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            ProcessResult(context);

            await next();
        }

        void ProcessResult(ResultExecutingContext context)
        {
            if (!_propNamesToSerialize.Any() || context.Result is not ObjectResult objectResult)
                return;

            var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();

            var serializerSettings = jsonOptions is null ?
                new JsonSerializerSettings() :
                NewtonsoftJsonSerializerSettingsHelper.DeepCopy(jsonOptions.Value.SerializerSettings);

            var modelType = GetModelType(objectResult.Value.GetType());

            serializerSettings.ContractResolver = new NewtonsoftJsonIgnoreContractResolver(
                serializerSettings.ContractResolver,
                modelType,
                _propNamesToSerialize);

            var mvcOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();

            var jsonFormatter = new NewtonsoftJsonOutputFormatter(
                serializerSettings,
                ArrayPool<char>.Shared,
                mvcOptions is not null ? mvcOptions.Value : new MvcOptions());

            objectResult.Formatters.Add(jsonFormatter);
        }

        static Type GetModelType(Type type)
            => type.IsGenericType ? type.GetGenericArguments().FirstOrDefault() : type;
    }
}
