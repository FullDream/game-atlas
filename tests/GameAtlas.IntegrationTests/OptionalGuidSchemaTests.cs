using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class OptionalGuidSchemaTests
{
	[Fact]
	public async Task NullableGuidPatchFieldsPreserveNullForResetOperations()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

		foreach (var name in new[] { "UpdateCategoryRequest", "UpdateCategoryGroupRequest" })
		{
			var schema = schemas.GetProperty(name);

			foreach (var field in new[] { "iconAssetId", "beforeId" })
			{
				var property = schema.GetProperty("properties").GetProperty(field);

				Assert.Contains(property.GetProperty("type").EnumerateArray(), type => type.GetString() == "null");
				Assert.Equal("uuid", property.GetProperty("format").GetString());
				Assert.False(schema.TryGetProperty("required", out var required) && required.EnumerateArray().Any(value => value.GetString() == field));
			}
		}

		Assert.Equal("string", schemas.GetProperty("UpdateMapRequest").GetProperty("properties").GetProperty("slug").GetProperty("type").GetString());
	}
}
