using System;

namespace Monq.Tools.MvcExtensions.Extensions
{
    /// <summary>
    /// Методы расширения для работы с объектами.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Проверка на Null или DBNull.
        /// </summary>
        /// <param name="value">Тестируемое значение.</param>
        /// <returns>True если объект Null or DBNull.</returns>
        public static bool IsNull(this object? value) =>
            value is null || value == DBNull.Value;
    }
}
