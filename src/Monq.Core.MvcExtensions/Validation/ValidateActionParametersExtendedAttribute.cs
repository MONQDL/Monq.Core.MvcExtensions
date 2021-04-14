
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Monq.Core.MvcExtensions.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.MvcExtensions.Validation
{
    /// <summary>
    /// Расширенная версия <see cref="ValidateActionParametersAttribute"/>, которая ловит <see cref="BadRequestObjectResult"/>
    /// и создает кастомный <see cref="DetailedErrorResponseViewModel{T}"/>.
    /// </summary>
    public sealed class ValidateActionParametersExtendedAttribute : ValidateActionParametersAttribute
    {
        /// <inheritdoc />
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (Validate(context))
                base.OnResultExecuting(context);
        }

        /// <inheritdoc />
        public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) =>
            !Validate(context) ? Task.CompletedTask : base.OnResultExecutionAsync(context, next);

        bool Validate(ResultExecutingContext context)
        {
            if (!(context is { Result: BadRequestObjectResult badRequest }))
                return true;

            if (!(badRequest is { Value: IDetailedErrorResponseViewModel detailedErrorResponse }))
                return true;

            var fieldsWithMessage = detailedErrorResponse.Fields
                .Select(field => CovertFieldsToJsonPropertiesWithErrorMessage(field, detailedErrorResponse.Message))
                .ToList();

            var resultObject = new JsonResult(new
            {
                Message = "Ошибка в теле запроса.",
                BodyFields = new JObject(fieldsWithMessage)
            }, new JsonSerializerSettings { ContractResolver = JsonResolver });
            context.Result = new BadRequestObjectResult(resultObject.Value);

            return true;
        }

        static JProperty CovertFieldsToJsonPropertiesWithErrorMessage(in string field, string message)
        {
            try
            {
                var jObj = JsonConvert.DeserializeObject<JObject>(field);

                // REM: Пока поддержка только 1 уровня вложенных объектов.
                var innerProp = jObj.Properties().First();
                var innerPropContent = ((JObject)innerProp.Value).Properties();

                return new JProperty(ConvertToCamelCase(innerProp.Name),
                    new JObject(innerPropContent.Select(x =>
                    {
                        ((JArray)x.Value).Add(message);
                        return x;
                    })));
            }
            catch (JsonReaderException)
            {
                return new JProperty(ConvertToCamelCase(field), new JArray(message));
            }
        }

        static string ConvertToCamelCase(string name)
            => char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
