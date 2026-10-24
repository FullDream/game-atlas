using GameAtlas.Domain.Maps;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps;

public static class MapQueryExtensions
{
	public static IQueryable<Map> WithStyles(this IQueryable<Map> maps) =>
		maps.Include(map => map.Styles).ThenInclude(style => style.Source);

	public static IQueryable<Map> ForGame(this IQueryable<Map> maps, Guid gameId) =>
		maps.Where(map => map.GameId == gameId);

	public static IQueryable<Map> ForMap(this IQueryable<Map> maps, Guid gameId, Guid mapId) =>
		maps.Where(map => map.GameId == gameId && map.Id == mapId);
}
