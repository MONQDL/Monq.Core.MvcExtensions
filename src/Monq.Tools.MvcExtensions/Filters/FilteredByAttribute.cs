using System;

namespace Monq.Tools.MvcExtensions.Filters
{
    /// <summary>
    /// Атрибут позволяет указать поле, по которому будет вестись фильтрация.
    /// </summary>
    public class FilteredByAttribute : Attribute
    {
        /// <summary>
        /// Название поля, по которому будет вестись фильтрация.
        /// </summary>
        public string FilteredProperty { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FilteredByAttribute"/>.
        /// </summary>
        /// <param name="FilteredProperty">Название поля, по которому будет вестись фильтрация.</param>
        public FilteredByAttribute(string FilteredProperty)
        {
            this.FilteredProperty = FilteredProperty;
        }
    }
}
