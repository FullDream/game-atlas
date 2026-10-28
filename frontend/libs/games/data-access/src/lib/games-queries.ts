import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import { queryOptions } from '@tanstack/angular-query-experimental'
import { toPromise } from '@game-atlas/shared/util'
import { GamesService } from './generated/games/games.service'
import type { ListGamesParams, PaginatedResultOfGame } from './generated/models'

type GamesListOptions = ReturnType<typeof queryOptions<PaginatedResultOfGame, HttpErrorResponse>>

@Service()
export class GamesQueries {
	readonly #api = inject(GamesService)

	list(params: ListGamesParams = {}): GamesListOptions {
		const normalized: ListGamesParams = {
			...params,
			offset: params.offset ?? 0,
			limit: params.limit ?? 20,
			search: params.search?.trim() || undefined,
			sortDirection: params.sortDirection ?? (params.sortBy ? 'asc' : undefined),
		}

		return queryOptions<PaginatedResultOfGame, HttpErrorResponse>({
			queryKey: ['games', 'list', normalized],
			queryFn: ({ signal }) => toPromise(this.#api.listGames(normalized), signal),
		})
	}
}
