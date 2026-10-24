using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class SqlitePersistenceFailureClassifier : IPersistenceFailureClassifier
{
	public bool IsMapNameConflict(Exception exception) => IsUniqueConflict(exception,
		"Maps.GameId, Maps.NormalizedName");

	public bool IsMapStyleNameConflict(Exception exception) =>
		IsUniqueConflict(exception, "MapStyles.MapId, MapStyles.NormalizedName");

	private static bool IsUniqueConflict(Exception exception, string columns) => exception is DbUpdateException
	{
		InnerException: SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 } sqlite
	} && sqlite.Message == $"SQLite Error 19: 'UNIQUE constraint failed: {columns}'.";

	public bool IsCategoryNameConflict(Exception exception) =>
		exception is DbUpdateException
		{
			InnerException: SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 } sqlite
		} && sqlite.Message ==
		"SQLite Error 19: 'UNIQUE constraint failed: Categories.CategoryGroupId, Categories.NormalizedName'.";

	public bool IsCategoryGroupNameConflict(Exception exception) =>
		exception is DbUpdateException
		{
			InnerException: SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 } sqlite
		} && sqlite.Message ==
		"SQLite Error 19: 'UNIQUE constraint failed: CategoryGroups.MapId, CategoryGroups.NormalizedName'.";

	public bool IsGameSlugConflict(Exception exception) =>
		exception is DbUpdateException
		{
			InnerException: SqliteException
			{
				SqliteErrorCode: 19,
				SqliteExtendedErrorCode: 2067
			} sqlite
		} && sqlite.Message == "SQLite Error 19: 'UNIQUE constraint failed: Games.Slug'.";
}
