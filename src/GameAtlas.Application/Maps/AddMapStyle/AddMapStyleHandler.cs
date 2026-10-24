using GameAtlas.Application.Maps.Uploads;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.AddMapStyle;

public sealed class AddMapStyleHandler(MapUploadAdmission admission) : ICommandHandler<AddMapStyle, Result<MapDto>>
{
	public async ValueTask<Result<MapDto>> Handle(AddMapStyle command, CancellationToken cancellationToken) =>
		await admission.AdmitAsync(command.GameId, command.MapId, command.SessionId, command.Upload, cancellationToken);
}
