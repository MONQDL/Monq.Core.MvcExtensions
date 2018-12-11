using Monq.Tools.MvcExtensions.Models;
using Monq.Tools.MvcExtensions.Extensions;
using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class FilterByZabbixKeyExtensions
    {
        [Fact(DisplayName = "Проверка фильтра по Zabbixkey.")]
        public void ShouldProperlyFilterByZabbixKey()
        {
            var list = Enumerable.Range(0, 12).Select(x => new ValueViewModel { Id = x, ZabbixId = x / 4 + 1, ElementId = x % 4 });
            var slectedItems = list.Where(x => x.ZabbixId == 1).Take(2);
            var slectedItemIds = slectedItems.Select(x => x.Id);
            var filterKeys = slectedItems.Select(x => new ZabbixKey(x.ZabbixId, x.ElementId));

            var result = list.AsQueryable()
                .FilterByZabbixKey(filterKeys, x => x.ZabbixId, x => x.ElementId)
                .ToList();
            Assert.Equal(slectedItems.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, slectedItemIds));
        }

        [Fact(DisplayName = "Проверка фильтра по ZabbixKey (Несколько zabbixId).")]
        public void ShouldProperlyFilterByZbbixKeys()
        {
            var list = Enumerable.Range(0, 12).Select(x => new ValueViewModel { Id = x, ZabbixId = x / 4 + 1, ElementId = x % 4 });
            var slectedItems = list.Where(x => x.ZabbixId == 1).Take(2)
                .Union(list.Where(x => x.ZabbixId == 2).Take(2));

            var slectedItemIds = slectedItems.Select(x => x.Id);
            var filterKeys = slectedItems.Select(x => new ZabbixKey(x.ZabbixId, x.ElementId));

            var result = list.AsQueryable()
                .FilterByZabbixKey(filterKeys, x => x.ZabbixId, x => x.ElementId)
                .ToList();
            Assert.Equal(slectedItems.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, slectedItemIds));
        }

        [Fact(DisplayName = "Проверка фильтра по ConnectorKey.")]
        public void ShouldProperlyFilterByConnectorKey()
        {
            var list = Enumerable.Range(0, 12).Select(x => new ValueViewModel { Id = x, ZabbixId = x / 4 + 1, StringElementId = (x % 4).ToString() });
            var slectedItems = list.Where(x => x.ZabbixId == 1).Take(2);
            var slectedItemIds = slectedItems.Select(x => x.Id);
            var filterKeys = slectedItems.Select(x => new ConnectorKey(x.ZabbixId, x.StringElementId));

            var result = list.AsQueryable()
                .FilterByConnectorKey(filterKeys, x => x.ZabbixId, x => x.StringElementId)
                .ToList();
            Assert.Equal(slectedItems.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, slectedItemIds));
        }

        [Fact(DisplayName = "Проверка фильтра по ConnectorKey (Несколько ConnectorKey).")]
        public void ShouldProperlyFilterByConnectorKeys()
        {
            var list = Enumerable.Range(0, 12).Select(x => new ValueViewModel { Id = x, ZabbixId = x / 4 + 1, StringElementId = (x % 4).ToString() });
            var slectedItems = list.Where(x => x.ZabbixId == 1).Take(2)
                .Union(list.Where(x => x.ZabbixId == 2).Take(2));

            var slectedItemIds = slectedItems.Select(x => x.Id);
            var filterKeys = slectedItems.Select(x => new ConnectorKey(x.ZabbixId, x.StringElementId));

            var result = list.AsQueryable()
                .FilterByConnectorKey(filterKeys, x => x.ZabbixId, x => x.StringElementId)
                .ToList();
            Assert.Equal(slectedItems.Count(), result.Count);
            Assert.All(result, x => Assert.Contains(x.Id, slectedItemIds));
        }
    }
}