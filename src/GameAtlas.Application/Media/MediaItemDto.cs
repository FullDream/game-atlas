using System.Text.Json.Serialization;

namespace GameAtlas.Application.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MediaFolderDto), "folder")]
[JsonDerivedType(typeof(ImageAssetDto), "image")]
[JsonDerivedType(typeof(VideoAssetDto), "video")]
public abstract record MediaItemDto(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
