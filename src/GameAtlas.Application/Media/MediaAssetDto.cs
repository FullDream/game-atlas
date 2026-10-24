using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GameAtlas.Application.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ImageAssetDto), "image")]
[JsonDerivedType(typeof(VideoAssetDto), "video")]
public abstract record MediaAssetDto(Guid Id, Guid? FolderId, string Name,
	[property: Description("Public static URL with GET/HEAD, validators and byte ranges; no database lookup. " +
		"Crash-orphaned files may remain accessible by known URL.")]
	string Url, string ContentType,
	long ByteLength, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
	: MediaItemDto(Id, Name, CreatedAt, UpdatedAt);
