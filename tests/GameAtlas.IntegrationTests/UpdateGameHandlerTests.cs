using GameAtlas.Application.Games.UpdateGame;
using GameAtlas.Domain;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using GameAtlas.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class UpdateGameHandlerTests
{
	[Fact]
	public async Task UpdatesTrackedGameThroughMediatorAndReloadsCompleteState()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "game");
		var mapId = Guid.NewGuid();

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
			var result = await mediator.Send(new UpdateGame(game.Id, " Updated ", " updated ", " image ",
				mapId, GameStatus.Published));

			Assert.True(result.IsSuccess);
			Assert.Equal("Updated", result.Value.Name);
			Assert.Equal(GameStatus.Published, result.Value.Status);
		}

		await using var reloadScope = factory.Services.CreateAsyncScope();
		var stored = await reloadScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.SingleAsync();

		Assert.Equal(game.Id, stored.Id);
		Assert.Equal(game.CreatedAt, stored.CreatedAt);
		Assert.Equal("Updated", stored.Name);
		Assert.Equal("updated", stored.Slug);
		Assert.Equal("image", stored.Image);
		Assert.Equal(mapId, stored.MapId);
		Assert.Equal(GameStatus.Published, stored.Status);
		var returned = await reloadScope.ServiceProvider.GetRequiredService<IMediator>().Send(
			new UpdateGame(game.Id, MapId: Optional<Guid?>.From(null), Status: GameStatus.Draft));

		Assert.True(returned.IsSuccess);
		Assert.Null(returned.Value.MapId);
		Assert.Equal(GameStatus.Draft, returned.Value.Status);
	}

	[Fact]
	public async Task ReturnsNotFoundAndRejectsConflictsWithoutDirtyingTrackedGame()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "game");
		var other = await SeedAsync(factory, "other");
		await using var scope = factory.Services.CreateAsyncScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var missing = await mediator.Send(new UpdateGame(Guid.NewGuid(), Name: "New"));
		var conflict = await mediator.Send(new UpdateGame(game.Id, Name: "New", Slug: " other "));
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Contains(missing.Errors!, error => error.Type == ErrorType.NotFound);
		Assert.Contains(conflict.Errors!, error => error.Type == ErrorType.Conflict);
		Assert.All(context.ChangeTracker.Entries<Game>(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
		var stored = await context.Games.OrderBy(item => item.Slug).ToArrayAsync();

		Assert.Equal(new[] { "game", "other" }, stored.Select(item => item.Slug));
		Assert.Equal(game.UpdatedAt, stored.Single(item => item.Id == game.Id).UpdatedAt);
		Assert.Equal(other.UpdatedAt, stored.Single(item => item.Id == other.Id).UpdatedAt);
		Assert.All(stored, item => Assert.Equal("Game", item.Name));
	}

	[Fact]
	public async Task NoOpsAndInvalidCommandsNeverCallSaveChanges()
	{
		var observer = new UpdateSaveObserver();
		await using var factory = new GameApiFactory(interceptor: observer);

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "game");
		await using var scope = factory.Services.CreateAsyncScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var unchanged = await mediator.Send(new UpdateGame(game.Id, " Game ", " game ", " cover ",
			Optional<Guid?>.From(null), GameStatus.Draft));
		var invalid = await mediator.Send(new UpdateGame(game.Id, Name: "New", Image: " "));
		var empty = await mediator.Send(new UpdateGame(game.Id));

		Assert.True(unchanged.IsSuccess);
		Assert.Equal(game.UpdatedAt, unchanged.Value.UpdatedAt);
		Assert.True(invalid.IsFailure);
		Assert.True(empty.IsFailure);
		Assert.Equal(0, observer.UpdateSaves);
		var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.SingleAsync();

		Assert.Equal("Game", stored.Name);
		Assert.Equal(game.UpdatedAt, stored.UpdatedAt);
	}

	[Fact]
	public async Task RealUpdateUniqueViolationIsRecognizedWithoutPartialPersistence()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "game");
		await SeedAsync(factory, "taken");
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var tracked = await context.Games.AsTracking().SingleAsync(item => item.Id == game.Id);

			Assert.True(tracked.Update("New", "taken").IsSuccess);
			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

			Assert.True(new SqlitePersistenceFailureClassifier().IsGameSlugConflict(exception));
		}

		await using var reloadScope = factory.Services.CreateAsyncScope();
		var stored = await reloadScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.SingleAsync(item => item.Id == game.Id);

		Assert.Equal("game", stored.Slug);
		Assert.Equal("Game", stored.Name);
		Assert.Equal(game.UpdatedAt, stored.UpdatedAt);
	}

	[Fact]
	public async Task UnexpectedSaveFailureRemainsAnException()
	{
		var observer = new UpdateSaveObserver { Failure = new InvalidOperationException("private storage failure") };
		await using var factory = new GameApiFactory(interceptor: observer);

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "game");
		await using var scope = factory.Services.CreateAsyncScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await mediator.Send(new UpdateGame(game.Id, Name: "New")));
	}

	internal static async Task<Game> SeedAsync(GameApiFactory factory, string slug)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var game = Game.Create("Game", slug, "cover").Value!;

		context.Games.Add(game);
		await context.SaveChangesAsync();

		return game;
	}

	internal sealed class UpdateSaveObserver : SaveChangesInterceptor
	{
		public int UpdateSaves { get; private set; }
		public Exception? Failure { get; init; }

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
			DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (eventData.Context!.ChangeTracker.Entries<Game>().Any(entry => entry.State == EntityState.Modified))
			{
				UpdateSaves++;

				if (Failure is not null)
					throw Failure;
			}

			return ValueTask.FromResult(result);
		}
	}
}
