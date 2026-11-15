using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.GetPublicMap;

public sealed record GetPublicMap(string GameSlug, string MapSlug,
	ContentVisibility Visibility = ContentVisibility.PublishedOnly) : IQuery<Result<PublicMapDetailsDto>>;
