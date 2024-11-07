using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Monq.Core.MvcExtensions.JsonContractResolvers;

/// <summary>
/// Defines the custom, reflection-based JSON contract resolver used by Newtonsoft.Json.
/// </summary>
public class NewtonsoftJsonIgnoreContractResolver : DefaultContractResolver
{
    readonly HashSet<string> _propsNameToSerialize;
    readonly Type _type;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJsonIgnoreContractResolver"/> class.
    /// </summary>
    public NewtonsoftJsonIgnoreContractResolver(
        IContractResolver? contractResolver,
        Type type,
        IEnumerable<string> propNamesToSerialize)
    {
        _propsNameToSerialize = propNamesToSerialize.ToHashSet();
        _type = type;

        if (contractResolver is DefaultContractResolver defaultResolver)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            DefaultMembersSearchFlags = defaultResolver.DefaultMembersSearchFlags;
#pragma warning restore CS0618 // Type or member is obsolete
            SerializeCompilerGeneratedMembers = defaultResolver.SerializeCompilerGeneratedMembers;
            IgnoreSerializableInterface = defaultResolver.IgnoreSerializableInterface;
            IgnoreSerializableAttribute = defaultResolver.IgnoreSerializableAttribute;
            IgnoreIsSpecifiedMembers = defaultResolver.IgnoreIsSpecifiedMembers;
            IgnoreShouldSerializeMembers = defaultResolver.IgnoreShouldSerializeMembers;
            NamingStrategy = defaultResolver.NamingStrategy;
        }
    }

    /// <inheritdoc />
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (property.PropertyName is null)
            return property;

        if (!_propsNameToSerialize.Contains(property.PropertyName) && property.DeclaringType == _type)
            property.ShouldSerialize = _ => false;

        return property;
    }
}
