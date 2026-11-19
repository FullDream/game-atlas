using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class PostgresPersistenceFailureClassifier : IPersistenceFailureClassifier
{
	public bool IsMediaFolderNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_MediaFolders_NormalizedName") ||
		IsUniqueConflict(exception, "IX_MediaFolders_ParentId_NormalizedName");

	public bool IsMapSlugConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_Maps_GameId_Slug");

	public bool IsMapNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_Maps_GameId_NormalizedName");

	public bool IsMapStyleNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_MapStyles_MapId_NormalizedName");

	public bool IsCategoryNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_Categories_CategoryGroupId_NormalizedName");

	public bool IsCategoryGroupNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_CategoryGroups_MapId_NormalizedName");

	public bool IsGameSlugConflict(Exception exception) =>
		IsUniqueConflict(exception, "IX_Games_Slug");

	private static bool IsUniqueConflict(Exception exception, string constraint) =>
		exception is DbUpdateException
		{
			InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
		} && postgres.ConstraintName == constraint;
}
