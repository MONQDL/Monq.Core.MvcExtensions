using Newtonsoft.Json;

namespace Monq.Core.MvcExtensions.Helpers
{
    /// <summary>
    /// Helper to work with Newtonsoft.Json.JsonSerializerSettings.
    /// </summary>
    public static class NewtonsoftJsonSerializerSettingsHelper
    {
        /// <summary>
        /// Create a deep copy for a JsonSerializerSettings object.
        /// </summary>
        /// <param name="original">JsonSerializerSettings object to copy from.</param>
        /// <returns></returns>
        public static JsonSerializerSettings DeepCopy(JsonSerializerSettings original)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var result = new JsonSerializerSettings
            {
                ReferenceLoopHandling = original.ReferenceLoopHandling,
                MissingMemberHandling = original.MissingMemberHandling,
                ObjectCreationHandling = original.ObjectCreationHandling,
                NullValueHandling = original.NullValueHandling,
                DefaultValueHandling = original.DefaultValueHandling,
                PreserveReferencesHandling = original.PreserveReferencesHandling,
                TypeNameHandling = original.TypeNameHandling,
                MetadataPropertyHandling = original.MetadataPropertyHandling,
                TypeNameAssemblyFormat = original.TypeNameAssemblyFormat,
                TypeNameAssemblyFormatHandling = original.TypeNameAssemblyFormatHandling,
                ConstructorHandling = original.ConstructorHandling,
                ContractResolver = original.ContractResolver,
                EqualityComparer = original.EqualityComparer,
                ReferenceResolver = original.ReferenceResolver,
                ReferenceResolverProvider = original.ReferenceResolverProvider,
                TraceWriter = original.TraceWriter,
                Binder = original.Binder,
                SerializationBinder = original.SerializationBinder,
                Error = original.Error,
                Context = original.Context,
                DateFormatString = original.DateFormatString,
                MaxDepth = original.MaxDepth,
                Formatting = original.Formatting,
                DateFormatHandling = original.DateFormatHandling,
                DateTimeZoneHandling = original.DateTimeZoneHandling,
                DateParseHandling = original.DateParseHandling,
                FloatFormatHandling = original.FloatFormatHandling,
                FloatParseHandling = original.FloatParseHandling,
                StringEscapeHandling = original.StringEscapeHandling,
                Culture = original.Culture,
                CheckAdditionalContent = original.CheckAdditionalContent,
            };
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var converter in original.Converters)
            {
                result.Converters.Add(converter);
            }

            return result;
        }
    }
}
