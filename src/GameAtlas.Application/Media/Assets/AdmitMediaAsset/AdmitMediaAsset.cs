using GameAtlas.Application.Media.Inspection;
using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Assets.AdmitMediaAsset;

public sealed record AdmitMediaAsset(Guid UploadId, Guid AssetId, string? Name, Guid? FolderId,
	Optional<string?> Alt, Optional<Guid?> PreviewAssetId, long ByteLength, MediaInspection Inspection)
	: ICommand<Result<MediaAssetDto>>;
