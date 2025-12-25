using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monq.Core.MvcExtensions.Helpers;
using Monq.Core.MvcExtensions.JsonContractResolvers;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Attributes;

/// <summary>
/// Apply gRpc.FieldMask to the ActionResult.
/// </summary>
[RequiresUnreferencedCode("FieldMaskAttribute uses reflection and is not compatible with trimming.")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class FieldMaskAttribute : ActionFilterAttribute
{
    readonly string _parameterName;

    const string Separator = ",";
    const string RequestItemName = "propNamesToSerialize";
    const string DefaultParameterName = "fieldMask";

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMaskAttribute"/> class.
    /// </summary>
    public FieldMaskAttribute(string parameterName = DefaultParameterName)
    {
        _parameterName = parameterName;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ProcessArrayInput(context, _parameterName);
    }

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ProcessArrayInput(context, _parameterName);

        await next();
    }

    static void ProcessArrayInput(ActionExecutingContext context, string parameterName)
    {
        ActionFilterAttributeHelper.ProcessArrayInput(context, parameterName, Separator);
        if (context.ActionArguments[parameterName] is not string[] propNames)
            return;
        // REM: for handling parallel requests.
        context.HttpContext.Items[RequestItemName] = propNames;
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

    static void ProcessResult(ResultExecutingContext context)
    {
        var requestItem = context.HttpContext.Items[RequestItemName];
        if (requestItem is null)
            return;
        var propNamesToSerialize = ((string[])requestItem)
            // Fields with nested properties are not supported yet.
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Contains('.') ? x[..x.IndexOf('.')] : x)
            .ToArray();
        if (propNamesToSerialize.Length == 0 || context.Result is not ObjectResult objectResult)
            return;

        if (objectResult.Value is null)
            return;

        // System.Text.Json is used by default.
        if (IsSystemTextJsonSerializer(context, out var systemTextJsonOptions))
        {
            AddCustomSystemTextJsonFormatter(objectResult, systemTextJsonOptions, propNamesToSerialize);
            return;
        }

        if (IsNewtonsoftJsonSerializer(context, out var newtonsoftJsonOptions))
        {
            AddCustomNewtonsoftJsonFormatter(context, objectResult, newtonsoftJsonOptions, propNamesToSerialize);
            return;
        }
    }

    static void AddCustomNewtonsoftJsonFormatter(
        ResultExecutingContext context,
        ObjectResult objectResult,
        IOptions<MvcNewtonsoftJsonOptions> options,
        string[] propNamesToSerialize)
    {
        if (objectResult.Value is null)
            return;

        var serializerSettings = NewtonsoftJsonSerializerSettingsHelper.DeepCopy(options.Value.SerializerSettings);

        var modelType = GetModelType(objectResult.Value.GetType());

        serializerSettings.ContractResolver = new NewtonsoftJsonIgnoreContractResolver(
            serializerSettings.ContractResolver,
            modelType,
            propNamesToSerialize);

        var mvcOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();

        // TODO: Simplify after ending support for .NET 5 and .NET 6.
        var jsonFormatter = new NewtonsoftJsonOutputFormatter(
            serializerSettings,
            ArrayPool<char>.Shared,
            mvcOptions is not null ? mvcOptions.Value : new MvcOptions());

        objectResult.Formatters.Add(jsonFormatter);
    }

    static void AddCustomSystemTextJsonFormatter(
        ObjectResult objectResult,
        JsonSerializerOptions options,
        string[] propNamesToSerialize)
    {
        if (objectResult.Value is null)
            return;

        // Custom JsonResolver for System.Text.Json has been added in .NET 7.
        var modelType = GetModelType(objectResult.Value.GetType());

        var jsonOptions = new JsonSerializerOptions(options)
        {
            TypeInfoResolver = new SystemTextJsonIgnoreContractResolver(modelType, propNamesToSerialize)
        };
        var jsonFormatter = new SystemTextJsonOutputFormatter(jsonOptions);

        objectResult.Formatters.Add(jsonFormatter);
    }

    static bool IsNewtonsoftJsonSerializer(ResultExecutingContext context, [NotNullWhen(true)] out IOptions<MvcNewtonsoftJsonOptions>? options)
    {
        options = GetConfiguredOptions<MvcNewtonsoftJsonOptions>(context);
        return options is not null;
    }

    static bool IsSystemTextJsonSerializer(ResultExecutingContext context, [NotNullWhen(true)] out JsonSerializerOptions? options)
    {
        var jsonOptions = GetConfiguredOptions<JsonOptions>(context);
        options = jsonOptions?.Value.JsonSerializerOptions;
        return options is not null;
    }

    static IOptions<T>? GetConfiguredOptions<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
        (ResultExecutingContext context) where T : class
    {
        // REM: checking for IConfigureOptions, because getting IOptions always returns default object.
        var configured = context.HttpContext.RequestServices.GetService<IConfigureOptions<T>>();
        if (configured is null)
            return null;
        return context.HttpContext.RequestServices.GetService<IOptions<T>>();
    }

    static Type GetModelType(Type type)
        => type.IsGenericType ? type.GetGenericArguments().First() : type;
}
