using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.Filters
{
    /// <summary>
    /// Атрибут добавляет поддержку валидации входящей модели данных.
    /// </summary>
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Название параметра модели данных, которая проходит валидацию.
        /// </summary>
        readonly string _parameterName;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidateModelAttribute"/>.
        /// </summary>
        /// <param name="parameterName">Название параметра модели данных.</param>
        /// <param name="nullValidation">True - выполнить проверку на на null модели данных.</param>
        public ValidateModelAttribute(string parameterName, bool nullValidation = true)
        {
            _parameterName = parameterName;
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
            if (context.ActionArguments.ContainsKey(_parameterName) && context.ActionArguments[_parameterName] == null)
            {
                context.Result = new BadRequestObjectResult(new { message = "Пустое тело запроса." });
                return false;
            }

            if (context.ModelState.IsValid == false)
            {
                context.Result = new BadRequestObjectResult(new { message = "Неверная модель данных.", fields = new SerializableError(context.ModelState) });
                return false;
            }

            return true;
        }
    }
}
