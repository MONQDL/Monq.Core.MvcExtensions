using System.Collections.Generic;

namespace Monq.Core.MvcExtensions.ViewModels;

/// <summary>
/// Модель представления детального сообщения об ошибке.
/// </summary>
public sealed class DetailedErrorResponseViewModel<T> : ErrorResponseViewModel, IDetailedErrorResponseViewModel
    where T : class
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DetailedErrorResponseViewModel{T}"/>.
    /// </summary>
    /// <param name="postViewModel">Экземпляр принимаемой модели представления.</param>
    /// <param name="message">Сообщение об ошибке.</param>
    public DetailedErrorResponseViewModel(T postViewModel, string message) : base(message)
    {
        PostViewModel = postViewModel;
    }

    /// <summary>
    /// Экземпляр принимаемой модели представления.
    /// </summary>
    public T PostViewModel { get; }

    /// <summary>
    /// Поля из входящей модели представления, которые влияют на данную ошибку.
    /// </summary>
    public List<string> Fields { get; } = new List<string>();
}