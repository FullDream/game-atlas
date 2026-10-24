using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Media;

public sealed class MediaFolder
{
	public const int NameMaxLength = 200;

	private MediaFolder(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid? ParentId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<MediaFolder> Create(string? name, Guid? parentId = null)
	{
		var folder = new MediaFolder(Guid.CreateVersion7());
		var result = folder.Update(name, parentId);

		if (result.IsFailure)
			return result.Errors;

		folder.CreatedAt = folder.UpdatedAt;

		return folder;
	}

	public Result<bool> Update(Optional<string?> name = default, Optional<Guid?> parentId = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextParent = parentId.GetValueOrDefault(ParentId);

		if (string.IsNullOrEmpty(nextName) || nextName.Length > NameMaxLength)
			return MediaErrors.InvalidName;

		if (nextParent == Id)
			return MediaErrors.FolderCycle;

		if (nextName == Name && nextParent == ParentId)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		ParentId = nextParent;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}
}
