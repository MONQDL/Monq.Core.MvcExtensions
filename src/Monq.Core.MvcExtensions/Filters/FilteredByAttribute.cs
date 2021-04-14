using System;

namespace Monq.Core.MvcExtensions.Filters
{
    /// <summary>
    /// Атрибут позволяет указать поле, по которому будет вестись фильтрация.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FilteredByAttribute : Attribute
    {
        /// <summary>
        /// Название поля, по которому будет вестись фильтрация.
        /// </summary>
        public string FilteredProperty { get; protected set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FilteredByAttribute"/>.
        /// </summary>
        /// <param name="filteredProperty">Название поля, по которому будет вестись фильтрация.</param>
        public FilteredByAttribute(string filteredProperty) =>
            FilteredProperty = filteredProperty;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FilteredByAttribute"/>.
        /// </summary>
        /// <param name="filteredProperty">Название фильтруемых полей от корневого до дочернего.</param>
        public FilteredByAttribute(params string[] filteredProperty) =>
            FilteredProperty = string.Join(".", filteredProperty);

        /// <inheritdoc />
        public override object TypeId => this;
    }
}