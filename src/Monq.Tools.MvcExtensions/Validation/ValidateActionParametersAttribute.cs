using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Serialization;
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
                context.Result = resultObject;
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
                if (context.Controller != null)
                    ((ControllerBase)(context.Controller)).TryValidateModel(model);

                if (context.ModelState.IsValid == false)
                {
                    var resultObject = new JsonResult(new { message = "Неверная модель данных в теле запроса.",
                        bodyFields = new SerializableError(context.ModelState) }, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = _jsonResolver });
                    context.Result = resultObject;
                    return;
                }
            }
            else if (fromBodyParameter != null && !context.ActionArguments.ContainsKey(fromBodyParameter.Name))
            {
                context.Result = new BadRequestObjectResult(new { message = "Пустое тело запроса." });
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
    }
}