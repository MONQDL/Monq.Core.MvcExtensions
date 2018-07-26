using Monq.Tools.MvcExtensions.Extensions;
using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using Monq.Tools.MvcExtensions.Tests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class FilterByAttributeExtensionsTests
    {
        [Fact(DisplayName = "Проверка фильтра по полю числового типа.")]
        public void ShouldProperlyFilterByInt()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x });
            var filter = new TestFilterViewModel { Ids = new List<int> { 1, 5, 6 } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.Ids.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, filter.Ids));
        }

        [Fact(DisplayName = "Проверка фильтра по полю, которому задано несколько атрибутов фильтрации числового типа.")]
        public void ShouldProperlyFilterByMultipleFields()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x, Capacity = 10 - x });
            var filter = new TestFilterViewModel { IdCaps = new List<int> { 1, 5, 6 } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(5, result.Count);
            Assert.All(result, x => Assert.True(filter.IdCaps.Contains(x.Id) || filter.IdCaps.Contains(x.Capacity)));
        }

        [Fact(DisplayName = "Проверка фильтра по полю строкового типа.")]
        public void ShouldProperlyFilterByString()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Name = $"Name{x}" });
            var filter = new TestFilterViewModel { Names = new List<string> { "Name1", "Name5", "Name6" } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.Names.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Name, filter.Names));
        }

        [Fact(DisplayName = "Проверка фильтра по простому типу.")]
        public void ShouldProperlyFilterBySimpleType()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x, Enabled = x % 2 == 0 });
            var filter = new TestFilterViewModel { Enabled = true };
            var result = list.AsQueryable().FilterBy(filter).ToList();

            Assert.All(result, x => Assert.True(x.Enabled));
        }

        [Fact(DisplayName = "Проверка фильтра по простому типу (String).")]
        public void ShouldProperlyFilterBySimpleTypeString()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x, Name = $"Name{x % 2}" });
            var filter = new TestFilterViewModel { Name = "0" };
            var result = list.AsQueryable().FilterBy(filter).ToList();

            Assert.All(result, x => Assert.Contains(filter.Name, x.Name));
        }

        [Fact(DisplayName = "Проверка фильтра по простому типу (String), пустая строка.")]
        public void ShouldProperlyFilterBySimpleTypeStringEmpty()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x, Name = $"Name{x % 2}" });
            var filter = new TestFilterViewModel { Name = "" };
            var result = list.AsQueryable().FilterBy(filter).ToList();

            Assert.Equal(list.Count(), result.Count);
        }

        [Fact(DisplayName = "Проверить является ли фильтр пустым.")]
        public void ShouldProperlyCheckIsEmpty()
        {
            TestFilterViewModel filter = null;
            Assert.True(filter.IsEmpty());

            filter = new TestFilterViewModel();
            Assert.True(filter.IsEmpty());

            filter.Ids = Enumerable.Empty<int>();
            Assert.True(filter.IsEmpty());

            filter.Ids = new[] { 1 };
            Assert.False(filter.IsEmpty());

            var filter2 = new List<TestFilterViewModel>();
            Assert.True(filter2.IsEmpty());
        }

        [Fact(DisplayName = "Проверить соответствие модели фильтру.")]
        public void ShouldProperlyValidFilter()
        {
            AssertExtensions.AssertFilterIsValid<TestFilterViewModel, ValueViewModel>();
            //AssertExtensions.AssertFilterIsValid<BadFilterModel, ValueViewModel>();
        }
    }
}