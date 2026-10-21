using System.Text.Json;
using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Serialization.Optional;

internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
	public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return Optional<T>.From(default);

		var value = JsonSerializer.Deserialize<T>(ref reader, options);

		return Optional<T>.From(value);
	}

	public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
	{
		if (!value.IsSpecified)
			throw new JsonException("An unspecified Optional value cannot be serialized.");

		JsonSerializer.Serialize(writer, value.Value, options);
	}
}
