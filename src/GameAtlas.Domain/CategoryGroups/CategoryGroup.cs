using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.CategoryGroups;

public sealed class CategoryGroup
{
	private CategoryGroup(Guid id, Guid gameId)
	{
		Id = id;
		GameId = gameId;
	}

	public Guid Id { get; }
	public Guid GameId { get; }
	public string Name { get; private set; } = string.Empty;
	public string NormalizedName { get; private set; } = string.Empty;
	public int Order { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

	public static Result<CategoryGroup> Create(Guid gameId, string? name, int order)
	{
		name = name?.Trim();

		if (string.IsNullOrEmpty(name))
			return CategoryGroupErrors.NameRequired;

		if (order <= 0)
			return CategoryGroupErrors.InvalidOrder;

		var now = DateTimeOffset.UtcNow;

		return new CategoryGroup(Guid.CreateVersion7(), gameId)
		{
			Name = name,
			NormalizedName = NormalizeName(name),
			Order = order,
			CreatedAt = now,
			UpdatedAt = now
		};
	}

	public Result<bool> Update(Optional<string?> name = default, Optional<int?> order = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextOrder = order.GetValueOrDefault(Order);

		if (string.IsNullOrEmpty(nextName))
			return CategoryGroupErrors.NameRequired;

		if (nextOrder is null or <= 0)
			return CategoryGroupErrors.InvalidOrder;

		if (nextName == Name && nextOrder == Order)
			return false;

		Name = nextName;
		NormalizedName = NormalizeName(nextName);
		Order = nextOrder.Value;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}
}
