﻿using Monq.Core.MvcExtensions.Filters;
using System.Collections.Generic;

namespace Monq.Core.MvcExtensions.TestApp.ViewModels;

public class TestFilterViewModel
{
    [FilteredBy(nameof(ValueViewModel.Id))]
    public IEnumerable<int> Ids { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.Name))]
    public IEnumerable<string> Names { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.Id))]
    [FilteredBy(nameof(ValueViewModel.Capacity))]
    public IEnumerable<int> IdCaps { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.Enabled))]
    public bool? Enabled { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.Name))]
    public string Name { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.Child), nameof(ValueViewModel.Name))]
    public IEnumerable<string> ChildNames { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.ChildEnum), nameof(ValueViewModel.Name))]
    public IEnumerable<string> ChildNameEnums { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.ChildEnum), nameof(ValueViewModel.Id))]
    public IEnumerable<int> ChildIdEnums { get; set; } = null;

    [FilteredBy(nameof(ValueViewModel.ComputedProp))]
    public IEnumerable<long> Computed { get; set; }
}

public class BadFilterModel
{
    [FilteredBy(nameof(ValueViewModel.Id))]
    public IEnumerable<long> Ids { get; set; }

    [FilteredBy(nameof(ValueViewModel.Name))]
    public IEnumerable<long> Names { get; set; }

    [FilteredBy(nameof(RecursiveViewModel.SubObject))]
    public IEnumerable<string> Names2 { get; set; }
}