using System.Collections.Concurrent;
using System.Data.Common;
using GameAtlas.Application.Games.DeleteGame;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class DeleteGameHandlerTests
{
	[Theory]
	[InlineData(GameStatus.Draft)]
	[InlineData(GameStatus.Published)]
	public async Task DeletesWithOneDeleteStatementAndPreservesOtherGames(GameStatus status)
	{
		var capture = new DeleteCommandCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "target", status);
		var other = await SeedAsync(factory, "other", GameStatus.Published);

		capture.Commands.Clear();

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new DeleteGame(game.Id));

			Assert.True(result.IsSuccess);
			Assert.Single(capture.Commands, command => command.StartsWith("DELETE"));
			Assert.Empty(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ChangeTracker.Entries());
		}

		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		AssertSameGame(other, stored);
	}

	[Fact]
	public async Task MissingAndRepeatedDeletesReturnNotFoundWithoutChangingOtherGames()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await SeedAsync(factory, "target");
		var other = await SeedAsync(factory, "other");
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
			var missing = await mediator.Send(new DeleteGame(Guid.NewGuid()));
			var deleted = await mediator.Send(new DeleteGame(game.Id));
			var repeated = await mediator.Send(new DeleteGame(game.Id));

			Assert.Equal(DeleteGameErrors.NotFound, Assert.Single(missing.Errors!));
			Assert.True(deleted.IsSuccess);
			Assert.Equal(DeleteGameErrors.NotFound, Assert.Single(repeated.Errors!));
		}

		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		AssertSameGame(other, stored);
	}

	internal static async Task<Game> SeedAsync(
		GameApiFactory factory, string slug, GameStatus status = GameStatus.Draft)
	{
		await MediaTestData.EnsureCoverAsync(factory);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var game = Game.Create("Game", slug, MediaTestData.CoverId).Value!;

		Assert.True(game.Update(status: status).IsSuccess);
		context.Games.Add(game);
		await context.SaveChangesAsync();

		return game;
	}

	internal static void AssertSameGame(Game expected, Game actual)
	{
		Assert.Equal(expected.Id, actual.Id);
		Assert.Equal(expected.Name, actual.Name);
		Assert.Equal(expected.Slug, actual.Slug);
		Assert.Equal(expected.CoverId, actual.CoverId);
		Assert.Equal(expected.Status, actual.Status);
		Assert.Equal(expected.CreatedAt, actual.CreatedAt);
		Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
	}

	private sealed class DeleteCommandCapture : DbCommandInterceptor
	{
		internal ConcurrentQueue<string> Commands { get; } = new();

		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
			CancellationToken cancellationToken = default)
		{
			Commands.Enqueue(command.CommandText);

			return ValueTask.FromResult(result);
		}

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			Commands.Enqueue(command.CommandText);

			return ValueTask.FromResult(result);
		}

		public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
			CancellationToken cancellationToken = default)
		{
			Commands.Enqueue(command.CommandText);

			return ValueTask.FromResult(result);
		}
	}
}
