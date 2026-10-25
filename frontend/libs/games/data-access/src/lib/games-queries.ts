import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import { queryOptions } from '@tanstack/angular-query-experimental'
import { firstValueFrom, fromEvent, mergeMap, takeUntil, throwError } from 'rxjs'
import { GamesService } from './generated/games/games.service'
import type { ListGamesParams, PaginatedResultOfGameDto } from './generated/models'

type GamesListOptions = ReturnType<typeof queryOptions<PaginatedResultOfGameDto, HttpErrorResponse>>

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

		return queryOptions<PaginatedResultOfGameDto, HttpErrorResponse>({
			queryKey: ['games', 'list', normalized],
			queryFn: ({ signal }) => this.#load(normalized, signal),
		})
	}

	#load(params: ListGamesParams, signal: AbortSignal): Promise<PaginatedResultOfGameDto> {
		if (signal.aborted) {
			return Promise.reject(new DOMException('The request was aborted.', 'AbortError'))
		}

		const aborted = fromEvent(signal, 'abort').pipe(
			mergeMap(() => throwError(() => new DOMException('The request was aborted.', 'AbortError'))),
		)

		return firstValueFrom(this.#api.listGames(params).pipe(takeUntil(aborted)))
	}
}
