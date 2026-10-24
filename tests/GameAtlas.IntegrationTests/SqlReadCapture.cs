using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameAtlas.IntegrationTests;

internal sealed class SqlReadCapture : DbCommandInterceptor
{
	internal bool Enabled { get; set; }
	internal ConcurrentQueue<string> Commands { get; } = new();

	public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
		CommandEventData eventData, InterceptionResult<DbDataReader> result,
		CancellationToken cancellationToken = default)
	{
		if (Enabled)
			Commands.Enqueue(command.CommandText);

		return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
	}
}
