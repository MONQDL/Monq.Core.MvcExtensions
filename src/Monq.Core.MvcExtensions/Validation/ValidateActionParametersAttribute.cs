using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Validation;

/// <summary>
/// Validates all the validation attributes placed directly on action method parameters.
/// Code based on repo https://github.com/markvincze/rest-api-helpers.
/// </summary>
/// <remarks>
/// The framework by default doesn't evaluate the validation attributes put directly on action method parameters. It only evaluates the attributes put on the properties of the model types.
/// This filter validates the attributes placed directly on action method parameters, and adds all the validation errors to the ModelState collection.
/// </remarks>
public class ValidateActionParametersAttribute : ActionFilterAttribute
{
    internal static readonly CamelCasePropertyNamesContractResolver _jsonResolver = new()
    {
        NamingStrategy = new CamelCaseNamingStrategy
        {
            ProcessDictionaryKeys = true
        }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateActionParametersAttribute"/> class.
    /// </summary>
    public ValidateActionParametersAttribute() => Order = 1;

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!Validate(context))
            return;

        base.OnActionExecuting(context);
    }

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Validate(context))
            return;

        await next();
    }

    static bool Validate(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var parameters = descriptor.MethodInfo.GetParameters();
            context.ModelState.Clear();
            ValidateQuery(parameters, context);
            if (context.Result != null)
                return false;

            ValidateBody(parameters, context);
            if (context.Result != null)
                return false;
        }

        return true;
    }

    static void ValidateQuery(ParameterInfo[] parameters, ActionExecutingContext context)
    {
        var stringLocalizer = context.HttpContext?.RequestServices?.GetService<IStringLocalizer>();

        var queryParameters = parameters
            .Where(x => x.CustomAttributes.Any(z => z.AttributeType != typeof(FromBodyAttribute)));
        foreach (var parameter in queryParameters)
        {
            var argument = context.ActionArguments.ContainsKey(parameter.Name)
                ? context.ActionArguments[parameter.Name]
                : GetDefaultValue(parameter);

            EvaluateValidationAttributes(parameter, argument, context.ModelState, stringLocalizer);
        }
        if (context.ModelState.ErrorCount > 0)
        {
            var resultObject = new JsonResult(new
            {
                message = "Error in query parameters.",
                queryFields = new SerializableError(context.ModelState)
            }, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver });
            context.Result = new BadRequestObjectResult(resultObject.Value);
        }
    }

    static void ValidateBody(ParameterInfo[] parameters, ActionExecutingContext context)
    {
        var fromBodyParameter = parameters
            .FirstOrDefault(x => x.CustomAttributes.Any(z => z.AttributeType == typeof(FromBodyAttribute)));

        if (fromBodyParameter != null && context.ActionArguments.ContainsKey(fromBodyParameter.Name))
        {
            var model = context.ActionArguments[fromBodyParameter.Name];
            if (model == null)
            {
                context.Result = new BadRequestObjectResult(new { message = "Request body in empty." });
                return;
            }
            if (IsModelEmpty(model))
            {
                context.Result = new BadRequestObjectResult(new { message = "All fields in request body are empty." });
                return;
            }
            if (context.Controller == null)
            {
                context.Result = new BadRequestObjectResult(new { message = "Controller is not defined." });
                return;
            }

            var modelStateDictionary = ValidateModelRecursively(context, model);
            AddModelStateErrors(context, modelStateDictionary);

            if (!context.ModelState.IsValid)
            {
                var resultObject = new JsonResult(new
                {
                    message = "Wrong data model in request body.",
                    bodyFields = new SerializableError(context.ModelState)
                }, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver });
                context.Result = new BadRequestObjectResult(resultObject.Value);
            }
        }
        else if (fromBodyParameter != null && !context.ActionArguments.ContainsKey(fromBodyParameter.Name))
        {
            context.Result = new BadRequestObjectResult(new { message = "Wrong data model in request body." });
        }
    }

    static void EvaluateValidationAttributes(ParameterInfo parameter, object? argument, ModelStateDictionary modelState, IStringLocalizer? stringLocalizer)
    {
        var validationAttributes = parameter.CustomAttributes;

        foreach (var attributeData in validationAttributes)
        {
            var attributeInstance = CustomAttributeExtensions.GetCustomAttribute(parameter, attributeData.AttributeType);

            if (attributeInstance is ValidationAttribute validationAttribute)
            {
                var isValid = validationAttribute.IsValid(argument);
                if (!isValid && !string.IsNullOrWhiteSpace(parameter.Name))
                {
                    var errorMessage = validationAttribute.FormatErrorMessage(parameter.Name);
                    if (stringLocalizer is not null)
                        errorMessage = stringLocalizer[errorMessage];
                    modelState.AddModelError(parameter.Name, errorMessage);
                }
            }
        }
    }

    static bool IsModelEmpty(object model)
    {
        if (IsModelSimpleType(model))
            return false;

        return model.GetType().GetProperties().Select(prop => prop.GetValue(model, null)).All(value => value is null);
    }

    static bool IsModelSimpleType(object model)
    {
        var type = model.GetType();
        return IsTypeSimple(type);
    }

    static bool IsTypeSimple(Type type)
    {
        return type.IsValueType || type == typeof(string);
    }

    static ModelStateDictionary ValidateModelRecursively(ActionExecutingContext context, object model, ModelStateDictionary? modelStateDictionary = null)
    {
        modelStateDictionary ??= new ModelStateDictionary();

        //Если есть свойство, представленное в виде объекта или коллекции, помеченное атрибутом Required, то валидируем его.
        var isModelGeneric = model.GetType().IsGenericType;
        if (isModelGeneric)
        {
            var concreteGenericType = model.GetType().GetTypeInfo().GenericTypeArguments[0];
            if (!IsTypeSimple(concreteGenericType))
            {
                if (model is not IEnumerable<object> genericModel)
                {
                    modelStateDictionary.AddModelError("FromBody", "Failed to convert data model.");
                    return modelStateDictionary;
                }
                foreach (var item in genericModel)
                {
                    ValidateModel(context, item, ref modelStateDictionary);
                }
            }
        }
        else
        {
            ((ControllerBase)context.Controller).TryValidateModel(model);
            foreach (var (key, value) in context.ModelState)
            {
                modelStateDictionary.AddModelError(key, value.Errors.FirstOrDefault()?.ErrorMessage);
            }
            context.ModelState.Clear();
            ValidateModel(context, model, ref modelStateDictionary);
        }
        return modelStateDictionary;
    }

    static void ValidateModel(ActionExecutingContext context, object model, ref ModelStateDictionary modelStateDictionary)
    {
        foreach (var member in model.GetType().GetProperties())
        {
            if (!member.PropertyType.IsPublic)
                continue;

            var hasRequired = IsDefined(member, typeof(RequiredAttribute));
            if (!hasRequired)
                continue;

            var memberValue = member.GetValue(model, null);
            if (memberValue is null)
            {
                modelStateDictionary.AddModelError(nameof(member), "Value must not be null.");
                continue;
            }

            if (IsModelSimpleType(memberValue))
                continue;

            ValidateModelRecursively(context, memberValue, modelStateDictionary);
        }
    }

    static void AddModelStateErrors(ActionExecutingContext context, ModelStateDictionary modelStateDictionary)
    {
        foreach (var (key, value) in modelStateDictionary)
        {
            context.ModelState.AddModelError(key, value.Errors.FirstOrDefault()?.ErrorMessage);
        }
    }

    static object? GetDefaultValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        if (parameter.ParameterType == typeof(string))
            return string.Empty;

        return Activator.CreateInstance(parameter.ParameterType);
    }
}
