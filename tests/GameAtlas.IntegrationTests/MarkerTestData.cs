using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

internal static class MarkerTestData
{
	internal const string MixedMedia = """
		[{"type":"image","url":" image.png ","width":640,"height":480},
		 {"videoId":"Ab_cd-Ef012","type":"youtubeVideo"},
		 {"type":"video","url":" clip.mp4 ","preview":" preview.jpg "}]
		""";

	internal static string Path(Guid gameId) => $"/games/{gameId}/markers";

	internal static async Task<JsonElement> ReadAsync(
		HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
	{
		Assert.Equal(status, response.StatusCode);

		return await response.Content.ReadFromJsonAsync<JsonElement>();
	}

	internal static async Task<JsonElement> CreateAsync(HttpClient client, Guid gameId, Guid categoryId,
		string name = "Marker", JsonElement? media = null)
	{
		using var response = await client.PostAsJsonAsync(Path(gameId), new
		{
			categoryId,
			name,
			description = " Description ",
			position = new { x = -1.5, y = 0 },
			media = media ?? JsonSerializer.SerializeToElement(Array.Empty<object>())
		});

		return await ReadAsync(response);
	}

	internal static async Task<Marker> LoadAsync(GameApiFactory factory, Guid id)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Markers
			.Include(marker => marker.Media).SingleAsync(marker => marker.Id == id);
	}
}
