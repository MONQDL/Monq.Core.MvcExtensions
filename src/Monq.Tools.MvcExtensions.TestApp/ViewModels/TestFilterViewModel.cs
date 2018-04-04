using Monq.Tools.MvcExtensions.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Tools.MvcExtensions.TestApp.ViewModels
{
    public class TestFilterViewModel
    {
        [FilteredBy(nameof(ValueViewModel.Id))]
        public IEnumerable<int> Ids { get; set; }

        [FilteredBy(nameof(ValueViewModel.Name))]
        public IEnumerable<string> Names { get; set; }

        [FilteredBy(nameof(ValueViewModel.Id))]
        [FilteredBy(nameof(ValueViewModel.Capacity))]
        public IEnumerable<int> IdCaps { get; set; }
    }
}