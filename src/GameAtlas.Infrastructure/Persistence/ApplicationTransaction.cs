using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameAtlas.Infrastructure.Persistence;

internal sealed class ApplicationTransaction(IDbContextTransaction transaction) : IApplicationTransaction
{
	public Task CommitAsync(CancellationToken cancellationToken = default) =>
		transaction.CommitAsync(cancellationToken);

	public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
