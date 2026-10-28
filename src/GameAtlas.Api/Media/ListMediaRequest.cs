using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace GameAtlas.Api.Media;

public sealed record ListMediaRequest
{
	[FromQuery]
	public Guid? FolderId { get; init; }

	[FromQuery, DefaultValue(0)]
	public int? Offset { get; init; }

	[FromQuery, DefaultValue(24)]
	public int? Limit { get; init; }

	[FromQuery]
	public string? Search { get; init; }
}
