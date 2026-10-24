using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.Uploads;

public sealed class MapUploadAdmission(IApplicationDbContext context, IMapAdmissionSessions sessions,
	IMapStorage storage)
{
	public async Task<Result<MapDto>> AdmitAsync(Guid gameId, Guid mapId, Guid sessionId,
		StagedMapUpload upload, CancellationToken cancellationToken)
	{
		var prepared = await context.Maps.ForMap(gameId, mapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (prepared is null)
			return MapOperationErrors.NotFound;

		var result = prepared.AddStyle(upload.ToDomain());

		if (result.IsFailure)
			return result.Errors;

		var addition = result.Value;

		await sessions.RegisterAsync(sessionId, mapId, addition.Id, cancellationToken);

		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var current = await context.Maps.AsTracking().ForMap(gameId, mapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (current is null)
			return MapOperationErrors.NotFound;

		var appended = current.AppendPreparedStyle(addition);

		if (appended.IsFailure)
			return appended.Errors;

		storage.Promote(sessionId, upload.FileId, mapId, addition.Id);
		await context.SaveChangesAsync(cancellationToken);
		await sessions.CompleteAsync(sessionId, cancellationToken);
		var response = current.ToDto();

		await transaction.CommitAsync(cancellationToken);

		return response;
	}
}
