using System.ComponentModel;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Application.Games;
using Microsoft.AspNetCore.Mvc;

namespace GameAtlas.Api.Games;

public sealed class ListGamesRequest
{
	[FromQuery, DefaultValue(0)]
	public int? Offset { get; init; }

	[FromQuery, DefaultValue(20)]
	public int? Limit { get; init; }

	[FromQuery, Description("Trimmed literal name substring; ignores ASCII case. Empty means no filter.")]
	public string? Search { get; init; }

	[FromQuery, Description("name, createdAt, or updatedAt (case-insensitive). Omit for unspecified order.")]
	public GameSortBy? SortBy { get; init; }

	[FromQuery, Description("asc or desc (case-insensitive). Requires sortBy; defaults to asc with sortBy.")]
	public SortDirection? SortDirection { get; init; }
}
