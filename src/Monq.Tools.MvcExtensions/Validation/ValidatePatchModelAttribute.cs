using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Serialization;

namespace Monq.Tools.MvcExtensions.Validation
{
    /// <summary>
    /// Атрибут, валидирующий на пустоту модель данных Patch запроса.
    /// В случае, если модель пустая, либо в модели все свойства null (либо "" в случае с string свойствами), то возвращается BadRequest.
    /// </summary>
    public class ValidatePatchModelAttribute : ActionFilterAttribute
    {
        readonly CamelCasePropertyNamesContractResolver _jsonResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true } };
        public ValidatePatchModelAttribute()
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
                ValidateBody(parameters, context);
                if (context.Result != null)
                    return false;
            }
            return true;
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
            }
            else if (fromBodyParameter != null && !context.ActionArguments.ContainsKey(fromBodyParameter.Name))
            {
                context.Result = new BadRequestObjectResult(new { message = "Неверная модель данных в теле запроса." });
                return;
            }
        }

        bool IsModelEmpty(object model)
        {
            foreach (var prop in model.GetType().GetProperties())
            {
                var value = prop.GetValue(model, null);
                var type = prop.PropertyType;

                if (type == typeof(string))
                {
                    if (!string.IsNullOrEmpty(value.ToString()))
                        return false;
                }
                else if (value != null)
                    return false;
            }
            return true;
        }
    }
}
