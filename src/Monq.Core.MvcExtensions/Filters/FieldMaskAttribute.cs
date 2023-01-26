using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monq.Core.MvcExtensions.Helpers;
using Monq.Core.MvcExtensions.JsonContractResolvers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Filters
{
    /// <summary>
    /// Apply gRpc.FieldMask to the ActionResult.
    /// </summary>
    public class FieldMaskAttribute : ActionFilterAttribute
    {
        readonly string _parameterName;
        readonly List<string> _propNamesToSerialize;

        /// <summary>
        /// An array string separator.
        /// </summary>
        public string Separator { get; set; } = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldMaskAttribute"/> class.
        /// </summary>
        public FieldMaskAttribute(string parameterName = "fieldMask")
        {
            _parameterName = parameterName;

            _propNamesToSerialize = new List<string>();
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ClearPropertyNameSerializationList();

            ProcessArrayInput(context, _parameterName);
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ClearPropertyNameSerializationList();

            ProcessArrayInput(context, _parameterName);

            await next();
        }
        
        void ProcessArrayInput(ActionExecutingContext context, string parameterName)
        {
            ActionFilterAttributeHelper.ProcessArrayInput(context, parameterName, Separator);

            if (context.ActionArguments[parameterName] is string[] propNames)
                _propNamesToSerialize.AddRange(propNames);
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

            if (objectResult.Value is null)
                return;

            // TODO: Add detecting Json Formatter.
#if NET7_0_OR_GREATER
            if (IsSystemTextJsonSerializer(context, out JsonSerializerOptions systemTextJsonOptions))
                AddCustomSystemTextJsonFormatter(context, objectResult, systemTextJsonOptions);
#else
            if (IsNewtonSoftJsonSerializer(context, out IOptions<MvcNewtonsoftJsonOptions> newtonSoftJsonOptions))
                AddCustomNewtonsoftJsonFormatter(context, objectResult, newtonSoftJsonOptions);
#endif
        }

        void AddCustomNewtonsoftJsonFormatter(ResultExecutingContext context, ObjectResult objectResult, IOptions<MvcNewtonsoftJsonOptions> newtonSoftJsonOptions)
        {
            var serializerSettings = NewtonsoftJsonSerializerSettingsHelper.DeepCopy(newtonSoftJsonOptions.Value.SerializerSettings);

            var modelType = GetModelType(objectResult.Value.GetType());

            serializerSettings.ContractResolver = new NewtonsoftJsonIgnoreContractResolver(
                serializerSettings.ContractResolver,
                modelType,
                _propNamesToSerialize);

            var mvcOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();

            // TODO: Simplify after ending support for .NET 5.
            var jsonFormatter = new NewtonsoftJsonOutputFormatter(
                serializerSettings,
                ArrayPool<char>.Shared,
                mvcOptions is not null ? mvcOptions.Value : new MvcOptions());

            objectResult.Formatters.Add(jsonFormatter);
        }

        void AddCustomSystemTextJsonFormatter(ResultExecutingContext context, ObjectResult objectResult, JsonSerializerOptions systemTextJsonOptions)
        {
#if NET7_0_OR_GREATER
            // Custom JsonResolver for System.Text.Json has been added in .NET 7.
            var modelType = GetModelType(objectResult.Value.GetType());

            var options = new JsonSerializerOptions(systemTextJsonOptions)
            {
                TypeInfoResolver = new SystemTextJsonIgnoreContractResolver(modelType, _propNamesToSerialize)
            };
            var jsonFormatter = new SystemTextJsonOutputFormatter(options);

            objectResult.Formatters.Add(jsonFormatter);
#else
            throw new NotSupportedException("Custom formatting for System.Text.Json supported for .NET 7 or greater.");
#endif
        }

        static bool IsNewtonSoftJsonSerializer(ResultExecutingContext context, out IOptions<MvcNewtonsoftJsonOptions> options)
        {
            options = context.HttpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();

            return options is not null;
        }

        static bool IsSystemTextJsonSerializer(ResultExecutingContext context, out JsonSerializerOptions options)
        {
            var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<JsonOptions>>();
            options = jsonOptions.Value.JsonSerializerOptions;

            return options is not null;
        }
        static Type GetModelType(Type type)
            => type.IsGenericType ? type.GetGenericArguments().FirstOrDefault() : type;

        void ClearPropertyNameSerializationList() => _propNamesToSerialize.Clear();
    }
}
