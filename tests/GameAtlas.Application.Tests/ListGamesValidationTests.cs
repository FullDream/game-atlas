using GameAtlas.Application.Behaviors;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Application.Games;
using GameAtlas.Application.Games.ListGames;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Tests;

public class ListGamesValidationTests
{
	[Theory]
	[InlineData(0, 1, null, null)]
	[InlineData(0, 100, GameSortBy.Name, SortDirection.Asc)]
	[InlineData(2, 20, GameSortBy.CreatedAt, SortDirection.Desc)]
	[InlineData(0, 20, GameSortBy.UpdatedAt, null)]
	public async Task AcceptsSupportedOptions(int offset, int limit, GameSortBy? sortBy, SortDirection? direction)
	{
		var result = await new ListGamesValidation().ValidateAsync(new ListGames(offset, limit,
			SortBy: sortBy, SortDirection: direction));

		Assert.True(result.IsValid);
	}

	[Theory]
	[InlineData(-1, 20, null, null, "Offset")]
	[InlineData(0, 0, null, null, "Limit")]
	[InlineData(0, -1, null, null, "Limit")]
	[InlineData(0, 101, null, null, "Limit")]
	[InlineData(0, 20, (GameSortBy)(-1), null, "SortBy")]
	[InlineData(0, 20, (GameSortBy)99, null, "SortBy")]
	[InlineData(0, 20, null, SortDirection.Asc, "SortBy")]
	[InlineData(0, 20, GameSortBy.Name, (SortDirection)(-1), "SortDirection")]
	[InlineData(0, 20, GameSortBy.Name, (SortDirection)99, "SortDirection")]
	public async Task RejectsInvalidOptions(
		int offset, int limit, GameSortBy? sortBy, SortDirection? direction, string property)
	{
		var result = await new ListGamesValidation().ValidateAsync(new ListGames(offset, limit,
			SortBy: sortBy, SortDirection: direction));

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.PropertyName == property);
	}

	[Fact]
	public async Task BehaviorAggregatesErrorsAndSkipsHandler()
	{
		var behavior = new ValidationBehavior<ListGames, Result<PaginatedResult<GameDto>>>([new ListGamesValidation()]);
		var result = await behavior.Handle(new ListGames(-1, 0, SortBy: (GameSortBy)(-1), SortDirection: (SortDirection)(-1)),
			(_, _) => throw new InvalidOperationException("Handler must not run."), CancellationToken.None);

		Assert.True(result.IsFailure);
		Assert.Equal(4, result.Errors.Length);
	}

	[Fact]
	public void UsesAgreedDefaults()
	{
		var query = new ListGames();

		Assert.Equal(0, query.Offset);
		Assert.Equal(20, query.Limit);
		Assert.Null(query.SortBy);
		Assert.Null(query.SortDirection);
	}
}
