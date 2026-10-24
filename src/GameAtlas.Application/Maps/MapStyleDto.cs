using System.ComponentModel;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps;

public sealed record MapStyleDto(
	Guid Id,
	string Name,
	string BackgroundColor,
	MapProcessingStatus Status,
	int Progress,
	MapProcessingErrorDto? Error,
	[property: Description("Public immutable WebP tile URL with {z}/{x}/{y} placeholders. Static delivery " +
		"supports GET, HEAD, ETag/Last-Modified and 304 without database checks. Complete files may remain " +
		"accessible until cleanup after deletion; missing files return 404.")]
	string? TilesUrlTemplate,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
