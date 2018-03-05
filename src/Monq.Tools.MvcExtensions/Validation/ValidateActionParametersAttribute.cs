﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.Validation
{
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
        readonly CamelCasePropertyNamesContractResolver _jsonResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true } };
        public ValidateActionParametersAttribute()
        {
            Order = 1;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!Validate(context))
                return;

            base.OnActionExecuting(context);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!Validate(context))
                return;

            await next();
        }

        bool Validate(ActionExecutingContext context)
        {
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (descriptor != null)
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
        void ValidateQuery(ParameterInfo[] parameters, ActionExecutingContext context)
        {
            var queryParameters = parameters
                .Where(x => x.CustomAttributes.Any(z => z.AttributeType != typeof(FromBodyAttribute)));
            foreach (var parameter in queryParameters)
            {
                var argument = context.ActionArguments.ContainsKey(parameter.Name) ?
                    context.ActionArguments[parameter.Name] : null;

                EvaluateValidationAttributes(parameter, argument, context.ModelState);
            }
            if (context.ModelState.ErrorCount > 0)
            {
                var resultObject = new JsonResult(new
                {
                    message = "Ошибка в параметрах запроса.",
                    queryFields = new SerializableError(context.ModelState)
                }, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver });
                context.Result = new BadRequestObjectResult(resultObject.Value);
            }
        }

        void ValidateBody(ParameterInfo[] parameters, ActionExecutingContext context)
        {
            var fromBodyParameter = parameters
                .FirstOrDefault(x => x.CustomAttributes.Any(z => z.AttributeType == typeof(FromBodyAttribute)));

            if (fromBodyParameter != null && context.ActionArguments.ContainsKey(fromBodyParameter.Name))
            {
                var model = context.ActionArguments[fromBodyParameter.Name];
                if (model == null)
                {
                    context.Result = new BadRequestObjectResult(new { message = "Пустое тело запроса." });
                    return;
                }
                if (IsModelEmpty(model))
                {
                    context.Result = new BadRequestObjectResult(new { message = "Все поля в модели данных пустые." });
                    return;
                }
                if (context.Controller == null)
                {
                    context.Result = new BadRequestObjectResult(new { message = "Не определён контроллер." });
                    return;
                }
                
                var modelStateDictionary = ValidateModelRecursive(context, model);
                AddModelStateErrors(context, modelStateDictionary);

                if (context.ModelState.IsValid == false)
                {
                    var resultObject = new JsonResult(new { message = "Неверная модель данных в теле запроса.",
                        bodyFields = new SerializableError(context.ModelState) }, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver });
                    context.Result = new BadRequestObjectResult(resultObject.Value);
                    return;
                }
            }
            else if (fromBodyParameter != null && !context.ActionArguments.ContainsKey(fromBodyParameter.Name))
            {
                context.Result = new BadRequestObjectResult(new { message = "Неверная модель данных в теле запроса." });
                return;
            }
        }

        void EvaluateValidationAttributes(ParameterInfo parameter, object argument, ModelStateDictionary modelState)
        {
            var validationAttributes = parameter.CustomAttributes;

            foreach (var attributeData in validationAttributes)
            {
                var attributeInstance = CustomAttributeExtensions.GetCustomAttribute(parameter, attributeData.AttributeType);

                var validationAttribute = attributeInstance as ValidationAttribute;

                if (validationAttribute != null)
                {
                    var isValid = validationAttribute.IsValid(argument);
                    if (!isValid)
                    {
                        modelState.AddModelError(parameter.Name, validationAttribute.FormatErrorMessage(parameter.Name));
                    }
                }
            }
        }

        bool IsModelEmpty(object model)
        {
            if (IsModelSimpleType(model))
                return false;

            foreach (var prop in model.GetType().GetProperties())
            {
                var value = prop.GetValue(model, null);
                if (value != null)
                    return false;
            }
            return true;
        }

        bool IsModelSimpleType(object model)
        {
            var type = model.GetType();
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(decimal))
              || type.Equals(typeof(string))
              || type.Equals(typeof(DateTime));
        }

        ModelStateDictionary ValidateModelRecursive(ActionExecutingContext context, object model, ModelStateDictionary modelStateDictionary = null)
        {
            if (modelStateDictionary == null)
                modelStateDictionary = new ModelStateDictionary();

            //Если есть свойство, представленное в виде объекта или коллекции, помеченное атрибутом Required, то валидируем его.
            var isModelGeneric = model.GetType().IsGenericType;
            if (isModelGeneric)
            {
                foreach (var item in model as IEnumerable<object>)
                {
                    ValidateModel(context, item, ref modelStateDictionary);
                }
            }
            else
            {
                ((ControllerBase)(context.Controller)).TryValidateModel(model);
                foreach (var error in context.ModelState)
                {
                    var key = error.Key;
                    var value = error.Value;
                    modelStateDictionary.AddModelError(error.Key, value.Errors.FirstOrDefault()?.ErrorMessage);
                }
                context.ModelState.Clear();
                ValidateModel(context, model, ref modelStateDictionary);
            }
            return modelStateDictionary;
        }
        
        void ValidateModel(ActionExecutingContext context, object model, ref ModelStateDictionary modelStateDictionary)
        {
            foreach (var member in model.GetType().GetProperties())
            {
                if (!member.PropertyType.IsPublic)
                    continue;

                var hasRequired = IsDefined(member, typeof(RequiredAttribute));
                if (!hasRequired)
                    continue;

                var memberValue = member.GetValue(model, null);
                if (memberValue == null)
                {
                    modelStateDictionary.AddModelError(nameof(member), $"Значение должно быть отличным от null.");
                    continue;
                }

                if (IsModelSimpleType(memberValue))
                    continue;

                ValidateModelRecursive(context, memberValue, modelStateDictionary);
            }
        }

        void AddModelStateErrors(ActionExecutingContext context, ModelStateDictionary modelStateDictionary)
        {
            foreach (var error in modelStateDictionary)
            {
                var key = error.Key;
                var value = error.Value;
                context.ModelState.AddModelError(error.Key, value.Errors.FirstOrDefault()?.ErrorMessage);
            }
        }
    }
}