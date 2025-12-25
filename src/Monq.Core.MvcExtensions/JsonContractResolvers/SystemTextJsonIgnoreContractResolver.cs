using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Monq.Core.MvcExtensions.JsonContractResolvers;

/// <summary>
/// Defines the custom, reflection-based JSON contract resolver used by System.Text.Json.
/// </summary>
[RequiresUnreferencedCode("SystemTextJson resolver is incompatible with trimming.")]
public class SystemTextJsonIgnoreContractResolver : DefaultJsonTypeInfoResolver
{
    readonly HashSet<string> _propsNameToSerialize;
    readonly Type _type;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonIgnoreContractResolver"/> class.
    /// </summary>
    public SystemTextJsonIgnoreContractResolver(
        Type type,
        IEnumerable<string> propNamesToSerialize)
    {
        _propsNameToSerialize = propNamesToSerialize.ToHashSet();
        _type = type;
    }

    /// <inheritdoc/>
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);

        if (type != _type || typeInfo.Kind != JsonTypeInfoKind.Object)
            return typeInfo;

        foreach (var property in typeInfo.Properties)
        {
            if (!_propsNameToSerialize.Contains(property.Name))
                property.ShouldSerialize = (obj, value) => false;
        }

        return typeInfo;
    }
}
