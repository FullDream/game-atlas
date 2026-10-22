using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class SqlitePersistenceFailureClassifier : IPersistenceFailureClassifier
{
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
