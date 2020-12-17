using System;
using System.ComponentModel;
using System.Globalization;

namespace Monq.Tools.MvcExtensions.Extensions
{
    /// <summary>
    /// Расширения для работы с перечислениями.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Получить описание из атрибута <see cref="DescriptionAttribute"/> данного элемента перечисления.
        /// </summary>
        /// <typeparam name="T">Тип-перечисление.</typeparam>
        /// <param name="e">Элемент перечисления.</param>
        /// <returns>Содержимое атрибута <see cref="DescriptionAttribute"/> элемента перечисления.</returns>
        public static string GetDescription<T>(this T e) where T : Enum, IConvertible
        {
            var description = string.Empty;

            var type = e.GetType();
            var values = Enum.GetValues(type);

            foreach (int val in values)
            {
                if (val != e.ToInt32(CultureInfo.InvariantCulture))
                    continue;

                var memberInfo = type.GetMember(type.GetEnumName(val));
                var descriptionAttributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptionAttributes.Length > 0)
                    description = ((DescriptionAttribute)descriptionAttributes[0]).Description;

                break;
            }

            return description;
        }
    }
}
