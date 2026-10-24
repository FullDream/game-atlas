using GameAtlas.Api.Results;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.AddMapStyle;
using GameAtlas.Application.Maps.Storage;
using Mediator;

namespace GameAtlas.Api.Maps.Uploads;

internal sealed class MapUploadWorkflow(MapStyleMultipartReader styleReader,
	IMapAdmissionSessions sessions, IMediator mediator)
{
	public async Task<IResult> SingleAsync(Guid gameId, Guid mapId, HttpRequest request,
		CancellationToken cancellationToken)
	{
		var sessionId = await sessions.BeginAsync(cancellationToken);

		try
		{
			var result = await styleReader.ReadAsync(request, sessionId, cancellationToken);

			if (result.Upload is not { } upload)
				return result.Failure ?? TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);

			return new OutcomeResult<MapDto>(
				await mediator.Send(new AddMapStyle(gameId, mapId, sessionId, upload), cancellationToken));
		}
		finally
		{
			await sessions.AbandonAsync(sessionId);
		}
	}
}
