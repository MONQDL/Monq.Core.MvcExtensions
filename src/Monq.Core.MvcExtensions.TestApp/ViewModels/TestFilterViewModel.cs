using System.Collections.Generic;

namespace Monq.Core.MvcExtensions.TestApp.ViewModels;

//[Attributes.GenerateEmptyCheck]
public partial class TestFilterViewModel
{
    [Attributes.FilteredBy(nameof(ValueViewModel.Id))]
    public IEnumerable<int> Ids { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.Name))]
    public IEnumerable<string> Names { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.Id))]
    [Attributes.FilteredBy(nameof(ValueViewModel.Capacity))]
    public IEnumerable<int> IdCaps { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.Enabled))]
    public bool? Enabled { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.Name))]
    public string Name { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.Child), nameof(ValueViewModel.Name))]
    public IEnumerable<string> ChildNames { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.ChildEnum), nameof(ValueViewModel.Name))]
    public IEnumerable<string> ChildNameEnums { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.ChildEnum), nameof(ValueViewModel.Id))]
    public IEnumerable<int> ChildIdEnums { get; set; } = null;

    [Attributes.FilteredBy(nameof(ValueViewModel.ComputedProp))]
    public IEnumerable<long> Computed { get; set; }
}

public class BadFilterModel
{
    [Attributes.FilteredBy(nameof(ValueViewModel.Id))]
    public IEnumerable<long> Ids { get; set; }

    [Attributes.FilteredBy(nameof(ValueViewModel.Name))]
    public IEnumerable<long> Names { get; set; }

    [Attributes.FilteredBy(nameof(RecursiveViewModel.SubObject))]
    public IEnumerable<string> Names2 { get; set; }
}
