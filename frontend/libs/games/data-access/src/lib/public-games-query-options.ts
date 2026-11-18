import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import { CreateQueryOptions, keepPreviousData, QueryClient, queryOptions } from "@tanstack/angular-query"
import { toPromise } from '@game-atlas/shared/util'
import { PublicGamesService } from './generated/public-games/public-games.service'
import type {
	ListPublicGamesParams,
	PaginatedResultOfPublicGameCard,
	PublicGame,
	PublicGameCard,
} from './generated/models'
import { normalizeGamesParams } from './games-query-keys'

export type PublicGameDetailData = PublicGameCard & Partial<PublicGame>

const publicListsKey = ['public-games', 'list'] as const

@Service()
export class PublicGamesQueryOptions {
	readonly #api = inject(PublicGamesService)
	readonly #client = inject(QueryClient)

	getList(
		params: ListPublicGamesParams = {},
	): CreateQueryOptions<PaginatedResultOfPublicGameCard, HttpErrorResponse> {
		const normalized = normalizeGamesParams(params)

		return queryOptions<PaginatedResultOfPublicGameCard, HttpErrorResponse>({
			queryKey: [...publicListsKey, normalized],
			placeholderData: keepPreviousData,
			queryFn: ({ signal }) => toPromise(this.#api.listPublicGames(normalized), signal),
		})
	}

	getDetail(slug: string): CreateQueryOptions<PublicGameDetailData, HttpErrorResponse> {
		return queryOptions<PublicGameDetailData, HttpErrorResponse>({
			queryKey: ['public-games', 'detail', slug],
			retry: (failureCount, error) => error.status !== 404 && failureCount < 3,
			queryFn: ({ signal }) => toPromise(this.#api.getPublicGame(slug), signal),
			placeholderData: () => {
				const lists = this.#client.getQueriesData<PaginatedResultOfPublicGameCard>({
					queryKey: publicListsKey,
					type: 'all',
					predicate: query => query.state.status === 'success',
				})

				return lists
					.sort(
						([first], [second]) =>
							(this.#client.getQueryState(second)?.dataUpdatedAt ?? 0) -
							(this.#client.getQueryState(first)?.dataUpdatedAt ?? 0),
					)
					.flatMap(([, page]) => page?.items ?? [])
					.find(game => game.slug === slug)
			},
		})
	}
}
