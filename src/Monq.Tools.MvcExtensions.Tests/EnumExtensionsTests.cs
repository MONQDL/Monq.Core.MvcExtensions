using System.ComponentModel;
using Xunit;
using Monq.Core.MvcExtensions.Extensions;

namespace Monq.Core.MvcExtensions.Tests
{
    public class EnumExtensionsTests
    {
        const string _xDescription = "Ось Х, абсцисса";
        const string _yDescription = "Ось У, ордината";

        enum Axis
        {
            [Description(_xDescription)]
            X,

            [Description(_yDescription)]
            Y,

            Z
        }

        [Fact(DisplayName = "API EnumExtensions: Проверка корректного получения описания.")]
        public void ShouldProperlyGetDesctiption()
        {
            var xDescription = Axis.X.GetDescription();
            Assert.Equal(_xDescription, xDescription);

            var yDescription = Axis.Y.GetDescription();
            Assert.Equal(_yDescription, yDescription);
        }

        [Fact(DisplayName = "API EnumExtensions: Проверка корректного получения пустой строки для элемента без описания.")]
        public void ShouldProperlyGetEmptyStringForElementWithoutDesctiption()
        {
            var zDescription = Axis.Z.GetDescription();
            Assert.Equal(string.Empty, zDescription);
        }
    }
}
