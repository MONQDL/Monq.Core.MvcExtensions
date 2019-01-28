using Monq.Tools.MvcExtensions.Extensions;
using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact(DisplayName = "Проверка фильтра по вычисляемому полю.")]
        public void ShouldProperlyFilterByComputedProperty()
        {
            var list = Enumerable.Range(0, 9).Select(x => new ValueViewModel { Id = x, Capacity = 10 * x });
            var filter = new TestFilterViewModel { Computed = new long[] { 11, 44, 55 } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(result.Count, filter.Computed.Count());
            Assert.All(result, x => Assert.Contains(x.ComputedProp, filter.Computed));
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

            filter = new TestFilterViewModel { Enabled = true };
            Assert.False(filter.IsEmpty());

            filter = new TestFilterViewModel { Name = "test" };
            Assert.False(filter.IsEmpty());

            var filter2 = new List<TestFilterViewModel>();
            Assert.True(filter2.IsEmpty());
        }

        [Fact(DisplayName = "Получить полный путь свойства.")]
        public void ShouldProperlyValidGetFullPropertyName()
        {
            var name = ExpressionHelpers.GetFullPropertyName<ValueViewModel, string>(x => x.Name);
            Assert.Equal("Name", name);

            name = ExpressionHelpers.GetFullPropertyName<ValueViewModel, string>(x => x.Child.Name);
            Assert.Equal("Child.Name", name);

            name = ExpressionHelpers.GetFullPropertyName<ValueViewModel, string>(x => x.Child.Child.Name);
            Assert.Equal("Child.Child.Name", name);
        }

        [Fact(DisplayName = "Проверка фильтра по вложенному полю строкового типа.")]
        public void ShouldProperlyFilterByStringNested()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Name = $"Name{x}", Child = new ValueViewModel { Name = $"ChildName{x}" } }).Union(new[] { new ValueViewModel { Name = $"Name{20}", Child = null } });
            var filter = new TestFilterViewModel { ChildNames = new List<string> { "ChildName1", "ChildName5", "ChildName6" } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.ChildNames.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Child.Name, filter.ChildNames));
        }

        [Fact(DisplayName = "Проверка фильтра по вложенному полю типа Enumerable строкового типа.")]
        public void ShouldProperlyFilterByStringNestedEnum()
        {
            var list = Enumerable.Range(0, 10)
                .Select(x => new ValueViewModel { Name = $"Name{x}", ChildEnum = new[] { new ValueViewModel { Name = $"ChildName{x}" } } });
            //.Union(new[] { new ValueViewModel { Name = $"Name{20}", ChildEnum = null } });
            var filter = new TestFilterViewModel { ChildNameEnums = new[] { "ChildName1", "ChildName5", "ChildName6" } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.ChildNameEnums.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.ChildEnum, y => filter.ChildNameEnums.Contains(y.Name)));
        }
    }
}