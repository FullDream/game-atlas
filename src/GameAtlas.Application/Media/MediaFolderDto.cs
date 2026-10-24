using System.Text.Json.Serialization;

namespace GameAtlas.Application.Media;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MediaFolderDto), "folder")]
public record MediaFolderDto(Guid Id, Guid? ParentId, string Name, int ItemsCount,
	DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt) : MediaItemDto(Id, Name, CreatedAt, UpdatedAt);
