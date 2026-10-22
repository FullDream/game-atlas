using GameAtlas.Application.Behaviors;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Games;
using GameAtlas.Application.Games.ListGames;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Tests;

public class ListGamesValidationTests
{
	[Theory]
	[InlineData(0, 1, null, null)]
	[InlineData(0, 100, "Name", "ASC")]
	[InlineData(2, 20, "CreatedAt", "dEsC")]
	[InlineData(0, 20, "updatedAt", null)]
	public async Task AcceptsSupportedOptions(int offset, int limit, string? sortBy, string? direction)
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
	[InlineData(0, 20, "slug", null, "SortBy")]
	[InlineData(0, 20, "0", null, "SortBy")]
	[InlineData(0, 20, "", null, "SortBy")]
	[InlineData(0, 20, " ", null, "SortBy")]
	[InlineData(0, 20, null, "asc", "SortBy")]
	[InlineData(0, 20, "name", "", "SortDirection")]
	[InlineData(0, 20, "name", " ", "SortDirection")]
	[InlineData(0, 20, "name", "sideways", "SortDirection")]
	public async Task RejectsInvalidOptions(int offset, int limit, string? sortBy, string? direction, string property)
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
		var result = await behavior.Handle(new ListGames(-1, 0, SortBy: "bad", SortDirection: "bad"),
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
