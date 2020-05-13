using System.Collections.Generic;

namespace Monq.Tools.MvcExtensions.ViewModels
{
    public interface IDetailedErrorResponseViewModel
    {
        /// <summary>
        /// Поля из входящей модели представления, которые влияют на данную ошибку.
        /// </summary>
        List<string> Fields { get; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        string Message { get; set; }
    }
}