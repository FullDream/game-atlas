using System.Text.Json;
using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Serialization.Optional;

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var valueType = typeToConvert.GetGenericArguments()[0];

		var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);

		return (JsonConverter)Activator.CreateInstance(converterType)!;
	}
}
