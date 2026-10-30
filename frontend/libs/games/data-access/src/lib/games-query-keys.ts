import type { ListGamesParams } from './generated/models'

export const gamesKey = ['games'] as const
export const gamesListKey = ['games', 'list'] as const
export const getGameQueryKey = (id: string) => ['games', 'detail', id] as const

export function normalizeGamesParams(params: ListGamesParams = {}): ListGamesParams {
	return {
		...params,
		offset: params.offset ?? 0,
		limit: params.limit ?? 20,
		search: params.search?.trim() || undefined,
		sortDirection: params.sortDirection ?? (params.sortBy ? 'asc' : undefined),
	}
}

export const getGamesQueryKey = (
	params?: ListGamesParams,
): readonly ['games', 'list'] | readonly ['games', 'list', ListGamesParams] =>
	params === undefined ? gamesListKey : ([...gamesListKey, normalizeGamesParams(params)] as const)
