using Monq.Tools.MvcExtensions.Extensions;
using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class FilterByAttributeExtensionsTests
    {
        [Fact]
        public void ShouldProperlyFilterByInt()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x });
            var filter = new TestFilterViewModel { Ids = new List<int> { 1, 5, 6 } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.Ids.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, filter.Ids));
        }

        [Fact]
        public void ShouldProperlyFilterByMultipleFields()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Id = x, Capacity = 10 - x });
            var filter = new TestFilterViewModel { IdCaps = new List<int> { 1, 5, 6 } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(5, result.Count);
            Assert.All(result, x => Assert.True(filter.IdCaps.Contains(x.Id) || filter.IdCaps.Contains(x.Capacity)));
        }

        [Fact]
        public void ShouldProperlyFilterByString()
        {
            var list = Enumerable.Range(0, 10).Select(x => new ValueViewModel { Name = $"Name{x}" });
            var filter = new TestFilterViewModel { Names = new List<string> { "Name1", "Name5", "Name6" } };
            var result = list.AsQueryable().FilterBy(filter).ToList();
            Assert.Equal(filter.Names.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Name, filter.Names));
        }
    }
}