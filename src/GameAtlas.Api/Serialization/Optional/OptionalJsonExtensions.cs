using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GameAtlas.Api.Serialization.Optional;

public static class OptionalJsonExtensions
{
	public static JsonSerializerOptions AddOptionalSupport(this JsonSerializerOptions options)
	{
		options.Converters.Add(new OptionalJsonConverterFactory());

		options.TypeInfoResolver = CreateTypeInfoResolver(options.TypeInfoResolver);

		return options;
	}

	private static IJsonTypeInfoResolver CreateTypeInfoResolver(IJsonTypeInfoResolver? resolver)
	{
		resolver ??= new DefaultJsonTypeInfoResolver();

		return resolver.WithAddedModifier(OptionalJsonTypeInfoModifier.Modify);
	}
}
