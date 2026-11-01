using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class MapOpenApiTests
{
	[Fact]
	public async Task DocumentsNineOperationsAndStaticTileContractWithoutChangingMarkerUnions()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var paths = root.GetProperty("paths");
		var mapPaths = paths.EnumerateObject().Where(path => path.Name.StartsWith("/games/{gameId}/maps"))
			.Where(path => !path.Name.Contains("/markers") && !path.Name.Contains("/category-groups")).ToArray();
		var operations = mapPaths.SelectMany(path => path.Value.EnumerateObject())
			.Where(property => new[] { "get", "post", "patch", "delete", "put" }.Contains(property.Name)).ToArray();

		Assert.Equal(9, operations.Length);
		Assert.DoesNotContain(operations, operation => operation.Name == "put");
		Assert.Equal(new[]
		{
			"AddMapStyle", "CreateMap", "DeleteMap", "DeleteMapStyle", "GetMap", "ListMaps",
			"RetryMapStyle", "UpdateMap", "UpdateMapStyle"
		}, operations.Select(operation => operation.Value.GetProperty("operationId").GetString()).Order());
		var create = paths.GetProperty("/games/{gameId}/maps").GetProperty("post");
		var form = Resolve(root, create.GetProperty("requestBody").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema"));

		Assert.True(create.GetProperty("requestBody").GetProperty("required").GetBoolean());
		Assert.Equal(new[] { "name" },
			form.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
		Assert.Equal(new[] { "name" },
			form.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order());
		var add = paths.GetProperty("/games/{gameId}/maps/{mapId}/styles").GetProperty("post");
		var addSchema = Resolve(root, add.GetProperty("requestBody").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema"));
		var addProperties = addSchema.GetProperty("properties");

		Assert.Equal(new[] { "backgroundColor", "name", "sourceAssetId" },
			addProperties.EnumerateObject().Select(property => property.Name).Order());
		Assert.Equal("uuid", Resolve(root, addProperties.GetProperty("sourceAssetId")).GetProperty("format").GetString());
		Assert.Equal(200, addProperties.GetProperty("name").GetProperty("maxLength").GetInt32());
		Assert.Equal(new[] { "backgroundColor", "name", "sourceAssetId" },
			addSchema.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order());
		var responses = create.GetProperty("responses");

		foreach (var status in new[] { "200", "400", "404", "409", "413", "415", "422", "500" })
			Assert.True(responses.TryGetProperty(status, out _));

		var map = Resolve(root, responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
			.GetProperty("schema"));

		Assert.Equal(15, map.GetProperty("properties").EnumerateObject().Count());
		var style = Resolve(root, map.GetProperty("properties").GetProperty("styles").GetProperty("items"));

		Assert.Equal(new[]
		{
			"backgroundColor", "createdAt", "error", "id", "name", "progress",
			"source", "status", "tileGeometry", "tilesUrlTemplate", "updatedAt"
		}, style.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
		Assert.Equal(new[] { "queued", "processing", "ready", "failed" },
			Resolve(root, style.GetProperty("properties").GetProperty("status")).GetProperty("enum")
				.EnumerateArray().Select(item => item.GetString()));
		Assert.False(paths.TryGetProperty("/games/{gameId}/maps/{mapId}/events", out _));
		Assert.DoesNotContain(paths.EnumerateObject(), path => path.Name.Contains("/tiles/"));
		Assert.Contains("without database checks", style.GetProperty("properties").GetProperty("tilesUrlTemplate")
			.GetProperty("description").GetString());
		var patch = paths.GetProperty("/games/{gameId}/maps/{mapId}/styles/{styleId}").GetProperty("patch");
		var patchContent = patch.GetProperty("requestBody").GetProperty("content");

		Assert.True(patchContent.TryGetProperty("application/json", out _));
		Assert.True(patchContent.TryGetProperty("application/merge-patch+json", out _));
		var patchSchema = Resolve(root, patchContent.GetProperty("application/json").GetProperty("schema"));

		Assert.Equal(2, patchSchema.GetProperty("properties").EnumerateObject().Count());
		Assert.Equal(200,
			patchSchema.GetProperty("properties").GetProperty("name").GetProperty("maxLength").GetInt32());

		foreach (var operation in operations.Where(operation => operation.Name == "delete"))
			Assert.False(operation.Value.GetProperty("responses").GetProperty("204").TryGetProperty("content", out _));

		Assert.False(root.GetProperty("components").GetProperty("schemas").TryGetProperty("MarkerMediaRequest", out _));
	}

	private static JsonElement Resolve(JsonElement root, JsonElement schema)
	{
		while (schema.TryGetProperty("$ref", out var reference))
			schema = root.GetProperty("components").GetProperty("schemas")
				.GetProperty(reference.GetString()!.Split('/')[^1]);

		return schema;
	}
}
