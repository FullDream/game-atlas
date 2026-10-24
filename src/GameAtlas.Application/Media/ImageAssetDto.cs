using System.Text.Json.Serialization;

namespace GameAtlas.Application.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ImageAssetDto), "image")]
public record ImageAssetDto(Guid Id, Guid? FolderId, string Name, string Url, string ContentType,
	long ByteLength, double? Width, double? Height, string Alt, string? Placeholder,
	DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
	: MediaAssetDto(Id, FolderId, Name, Url, ContentType, ByteLength, CreatedAt, UpdatedAt);
