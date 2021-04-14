using Monq.Core.MvcExtensions.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;

namespace Monq.Core.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для принимаемых моделей представления.
    /// </summary>
    public static class ViewModelExtensions
    {
        /// <summary>
        /// Создать модель представления ошибки выполнения запроса с описанием для выбранных полей POST-модели.
        /// </summary>
        /// <param name="postViewModel">Экземпляр принимаемой модели представления в запросе.</param>
        /// <param name="message">Текст сообщения.</param>
        public static DetailedErrorResponseViewModel<T> CreateErrorResponseModel<T>(this T postViewModel, string message)
            where T : class, new() =>
            new DetailedErrorResponseViewModel<T>(postViewModel, message);

        /// <summary>
        /// Добавить поле POST-модели в модель представления ошибки.
        /// </summary>
        /// <param name="errorModel">Модель представления ошибки выполнения запроса.</param>
        /// <param name="propSelector">Селектор поля принимаемой модели представления в запросе.</param>
        public static DetailedErrorResponseViewModel<TModel> AddModelField<TModel, TField>(
            this DetailedErrorResponseViewModel<TModel> errorModel,
            Expression<Func<TModel, TField>> propSelector)
            where TModel : class
        {
            var memberName = propSelector.GetFullPropertyName();

            if (memberName.Contains('.'))
            {
                var members = memberName.Split('.');

                // REM: Пока поддержка только 1 уровня вложенных объектов.
                if (members.Length != 2)
                    return errorModel;

                var json = new JObject(
                    new JProperty(members[0],
                        new JObject(
                            new JProperty(members[1], new JArray()))));

                errorModel.Fields.Add(json.ToString());
                return errorModel;
            }

            if (!errorModel.Fields.Contains(memberName))
                errorModel.Fields.Add(memberName);

            return errorModel;
        }
    }
}
