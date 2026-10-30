using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.GetGame;

public sealed class GetGameHandler(IApplicationDbContext context) : IQueryHandler<GetGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(GetGame request, CancellationToken cancellationToken)
	{
		var game = await context.Games.AsNoTracking().Include(game => game.Image)
			.SingleOrDefaultAsync(game => game.Id == request.Id, cancellationToken);

		if (game is null)
			return GetGameErrors.NotFound;

		return game.ToDto();
	}
}
