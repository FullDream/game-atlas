using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MarkerTransactionTests
{
	[Theory]
	[InlineData("patch", "delete")]
	[InlineData("patch", "insert")]
	[InlineData("patch", "save")]
	[InlineData("create", "save")]
	[InlineData("delete", "save")]
	public async Task FailuresRollBackReopenedAggregateChildrenAndCounts(string operation, string stage)
	{
		var writeFailure = new FailDuringChildWrite(stage);
		var saveFailure = new FailAfterSave();
		await using var factory = new GameApiFactory(interceptor: stage == "save" ? saveFailure : writeFailure);

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var source = await CategoryTestData.SeedAsync(factory, group.Id, "Source");
		var destination = await CategoryTestData.SeedAsync(factory, group.Id, "Destination", 2);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, source.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));
		var id = created.GetProperty("id").GetGuid();
		var before = await MarkerTestData.LoadAsync(factory, id);

		writeFailure.Enabled = true;
		saveFailure.Enabled = true;
		using var response = operation switch
		{
			"create" => await client.PostAsJsonAsync(MarkerTestData.Path(game.Id),
				new
				{
					categoryId = destination.Id,
					name = "New",
					position = new { x = 0, y = 0 },
					media = new[] { new { type = "video", url = "new-video" } }
				}),
			"delete" => await client.DeleteAsync(MarkerTestData.Path(game.Id) + $"/{id}"),
			_ => await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{id}",
				new
				{
					categoryId = destination.Id,
					name = "Changed",
					position = new { x = 999 },
					media = new[] { new { type = "video", url = "replacement" } }
				})
		};

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		Assert.DoesNotContain("private marker", await response.Content.ReadAsStringAsync());
		var after = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(before.CategoryId, after.CategoryId);
		Assert.Equal(before.Name, after.Name);
		Assert.Equal(before.Position, after.Position);
		Assert.Equal(before.UpdatedAt, after.UpdatedAt);
		Assert.Equal(before.Media.Select(item => item.Id), after.Media.Select(item => item.Id));
		using var countsResponse = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var counts = await MarkerTestData.ReadAsync(countsResponse);

		Assert.Equal(1, counts[0].GetProperty("markersCount").GetInt32());
		Assert.Equal(0, counts[1].GetProperty("markersCount").GetInt32());
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Single(await db.Markers.ToArrayAsync());
		Assert.Equal(3, await db.Set<MarkerMedia>().CountAsync());
		Assert.Equal(game.UpdatedAt, (await db.Games.SingleAsync()).UpdatedAt);
		Assert.Equal(group.UpdatedAt, (await db.CategoryGroups.SingleAsync()).UpdatedAt);
		Assert.Equal(new[] { source.UpdatedAt, destination.UpdatedAt },
			await db.Categories.OrderBy(item => item.Order).Select(item => item.UpdatedAt).ToArrayAsync());

		if (stage != "save")
			Assert.True(writeFailure.Triggered);
	}

	[Fact]
	public async Task EqualOrderedMediaAndEmptyPositionNeverSave()
	{
		var interceptor = new FailAfterSave();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

		interceptor.Enabled = true;
		using var response = await client.PatchAsJsonAsync(
			MarkerTestData.Path(game.Id) + "/" + created.GetProperty("id").GetGuid(),
			new { position = new { }, media = created.GetProperty("media") });

		Assert.Equal(created.GetRawText(), (await MarkerTestData.ReadAsync(response)).GetRawText());
	}

	[Fact]
	public async Task ConcurrentTransfersKeepOneCompleteAggregateAndCorrectCounts()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var first = await CategoryTestData.SeedAsync(factory, group.Id, "First");
		var second = await CategoryTestData.SeedAsync(factory, group.Id, "Second", 2);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, first.Id);
		var id = created.GetProperty("id").GetGuid();
		var path = MarkerTestData.Path(game.Id) + $"/{id}";
		var requests = new[] { first.Id, second.Id }.Select(async destination =>
		{
			using var response = await client.PatchAsJsonAsync(path, new
			{
				categoryId = destination,
				name = destination.ToString(),
				media = new[] { new { type = "video", url = destination.ToString() } }
			});

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		});

		await Task.WhenAll(requests);
		var marker = await MarkerTestData.LoadAsync(factory, id);
		var video = Assert.IsType<VideoMedia>(Assert.Single(marker.Media));

		Assert.Equal(marker.CategoryId.ToString(), marker.Name);
		Assert.Equal(marker.Name, video.Url);
		Assert.Equal(marker.Id, video.MarkerId);
		using var countsResponse = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var counts = await MarkerTestData.ReadAsync(countsResponse);

		Assert.Equal(1, counts.EnumerateArray().Sum(item => item.GetProperty("markersCount").GetInt32()));
		await using var scope = factory.Services.CreateAsyncScope();

		Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Set<MarkerMedia>().CountAsync());
	}

	[Fact]
	public async Task ListsAndCountsHaveConstantQueryBudgetAsDataGrows()
	{
		var interceptor = new QueryCounter();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

		interceptor.Commands.Clear();
		using var smallList = await client.GetAsync(MarkerTestData.Path(game.Id));
		var small = await MarkerTestData.ReadAsync(smallList);
		var smallQueries = interceptor.Commands.Count;

		Assert.Single(small.EnumerateArray());
		Assert.InRange(smallQueries, 2, 3);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			for (var index = 0; index < 30; index++)
			{
				var marker = Marker.Create(category.Id, "Duplicate", Position.Create(index, -index).Value,
					media: [VideoMedia.Create("https://not-fetched.invalid/video").Value]);

				Assert.True(marker.IsSuccess);
				db.Markers.Add(marker.Value);
			}

			await db.SaveChangesAsync();
		}

		interceptor.Commands.Clear();
		using var largeList = await client.GetAsync(MarkerTestData.Path(game.Id));
		var large = await MarkerTestData.ReadAsync(largeList);

		Assert.Equal(31, large.GetArrayLength());
		Assert.Equal(smallQueries, interceptor.Commands.Count);
		interceptor.Commands.Clear();
		using var categoryList = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var counts = await MarkerTestData.ReadAsync(categoryList);

		Assert.Equal(31, counts[0].GetProperty("markersCount").GetInt32());
		Assert.Equal(2, interceptor.Commands.Count);
		Assert.Contains(interceptor.Commands, sql => sql.Contains("COUNT(*)", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(interceptor.Commands, sql => sql.Contains("MarkerMedia"));
	}

	private sealed class FailAfterSave : SaveChangesInterceptor
	{
		internal bool Enabled { get; set; }

		public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
			CancellationToken cancellationToken = default)
		{
			if (Enabled)
				throw new InvalidOperationException("private marker save failure");

			return ValueTask.FromResult(result);
		}
	}

	private sealed class FailDuringChildWrite(string stage) : DbCommandInterceptor
	{
		internal bool Enabled { get; set; }
		internal bool Triggered { get; private set; }

		public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
			CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
		{
			var prefix = stage == "delete" ? "DELETE FROM \"MarkerMedia\"" : "INSERT INTO \"MarkerMedia\"";

			if (Enabled && command.CommandText.Contains(prefix, StringComparison.Ordinal))
			{
				Triggered = true;
				await result.DisposeAsync();

				throw new InvalidOperationException("private marker child write failure");
			}

			return result;
		}
	}

	private sealed class QueryCounter : DbCommandInterceptor
	{
		internal ConcurrentQueue<string> Commands { get; } = new();

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			Commands.Enqueue(command.CommandText);

			return ValueTask.FromResult(result);
		}
	}
}
