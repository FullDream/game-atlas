using System.ComponentModel;

namespace GameAtlas.Api.Maps;

public sealed record MapUploadRequest
{
	[Description("Style name, trimmed and unique within this map ignoring case.")]
	public required string Name { get; init; }

	[Description("Viewer background color in #RGB or #RRGGBB notation.")]
	public required string BackgroundColor { get; init; }

	[Description("One still PNG or JPEG original, with the map's encoded dimensions.")]
	public required IFormFile File { get; init; }
}
