using System.Text.Json.Serialization;

namespace GameAtlas.Application.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VideoAssetDto), "video")]
public record VideoAssetDto(Guid Id, Guid? FolderId, string Name, string Url, string ContentType,
	long ByteLength, ImageAssetDto? Preview, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
	: MediaAssetDto(Id, FolderId, Name, Url, ContentType, ByteLength, CreatedAt, UpdatedAt);
