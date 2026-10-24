using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Assets.UpdateMediaAsset;

public sealed record UpdateMediaAsset(Guid Id, Optional<string?> Name, Optional<Guid?> FolderId,
	Optional<string?> Alt, Optional<Guid?> PreviewAssetId) : ICommand<Result<MediaAssetDto>>;
