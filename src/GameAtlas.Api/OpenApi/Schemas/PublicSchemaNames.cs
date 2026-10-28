using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace GameAtlas.Api.OpenApi.Schemas;

internal static class PublicSchemaNames
{
	public static string? CreateReferenceId(JsonTypeInfo typeInfo)
	{
		var referenceId = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

		return referenceId is null ? null : RemoveDtoSuffixes(referenceId, typeInfo.Type);
	}

	private static string RemoveDtoSuffixes(string referenceId, Type type)
	{
		var name = type.Name.Split('`')[0];

		if (name.EndsWith("Dto", StringComparison.Ordinal))
			referenceId = referenceId.Replace(name, name[..^3], StringComparison.Ordinal);

		foreach (var argument in type.GetGenericArguments())
			referenceId = RemoveDtoSuffixes(referenceId, argument);

		if (type.GetElementType() is { } elementType)
			referenceId = RemoveDtoSuffixes(referenceId, elementType);

		return referenceId;
	}
}
