import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import {
	CreateMutationOptions,
	CreateQueryOptions,
	keepPreviousData,
	mutationOptions,
	QueryClient,
	queryOptions,
} from '@tanstack/angular-query-experimental'
import { omit } from 'remeda'
import { toPromise } from '@game-atlas/shared/util'
import { GamesService } from './generated/games/games.service'
import type {
	CreateGameRequest,
	Game,
	ListGamesParams,
	PaginatedResultOfGame,
	UpdateGameRequest,
} from './generated/models'
import { getGameQueryKey, getGamesQueryKey, normalizeGamesParams } from './games-query-keys'

type WithId<T> = { id: string } & T
type GameUpdateContext = { previous: Game | undefined; optimistic: Game | undefined }

@Service()
export class GamesQueryOptions {
	readonly #api = inject(GamesService)
	readonly #client = inject(QueryClient)

	getList(params: ListGamesParams = {}): CreateQueryOptions<PaginatedResultOfGame, HttpErrorResponse> {
		const normalized = normalizeGamesParams(params)

		return queryOptions<PaginatedResultOfGame, HttpErrorResponse>({
			queryKey: getGamesQueryKey(normalized),
			staleTime: 30_000,
			placeholderData: keepPreviousData,
			queryFn: ({ signal }) => toPromise(this.#api.listGames(normalized), signal),
		})
	}

	getById(id: string): CreateQueryOptions<Game, HttpErrorResponse> {
		const source = this.#client
			.getQueryCache()
			.findAll({ queryKey: getGamesQueryKey() })
			.filter(query => query.state.status === 'success')
			.sort((a, b) => b.state.dataUpdatedAt - a.state.dataUpdatedAt)
			.find(query => (query.state.data as PaginatedResultOfGame).items.some(game => game.id === id))

		return queryOptions<Game, HttpErrorResponse>({
			queryKey: getGameQueryKey(id),
			staleTime: source?.state.isInvalidated ? 0 : 30_000,
			initialData: () =>
				(source?.state.data as PaginatedResultOfGame | undefined)?.items.find(game => game.id === id),
			initialDataUpdatedAt: () => source?.state.dataUpdatedAt,
			queryFn: ({ signal }) => toPromise(this.#api.getGame(id), signal),
		})
	}

	create(): CreateMutationOptions<Game, HttpErrorResponse, CreateGameRequest> {
		return mutationOptions({
			mutationKey: ['games', 'create'],
			mutationFn: variables => toPromise(this.#api.createGame(variables)),
			onSuccess: async game => {
				this.#client.setQueryData(getGameQueryKey(game.id), game)
				await this.#client.invalidateQueries({ queryKey: getGamesQueryKey() })
			},
		})
	}

	update(id?: string): CreateMutationOptions<Game, HttpErrorResponse, WithId<UpdateGameRequest>, GameUpdateContext> {
		return mutationOptions({
			mutationKey: ['games', 'update'],
			scope: id ? { id: id } : undefined,

			mutationFn: ({ id: gameId, ...patch }) => toPromise(this.#api.updateGame(gameId, patch)),
			onMutate: async ({ id: gameId, ...patch }) => {
				await this.#client.cancelQueries({ queryKey: getGameQueryKey(gameId) })
				const previous = this.#client.getQueryData<Game>(getGameQueryKey(gameId))
				const optimistic = previous ? { ...previous, ...omit(patch, ['imageAssetId']) } : undefined

				if (optimistic) this.#client.setQueryData(getGameQueryKey(gameId), optimistic)

				return { previous, optimistic: this.#client.getQueryData<Game>(getGameQueryKey(gameId)) }
			},
			onError: async (error, { id: gameId }, snapshot) => {
				const key = getGameQueryKey(gameId)

				if (snapshot?.previous && this.#client.getQueryData(key) === snapshot.optimistic) {
					this.#client.setQueryData(key, snapshot.previous)
				} else {
					await this.#client.invalidateQueries({ queryKey: key })
				}
			},
			onSuccess: async game => {
				await this.#client.cancelQueries({ queryKey: getGameQueryKey(game.id) })
				this.#client.setQueryData(getGameQueryKey(game.id), game)
				await this.#client.invalidateQueries({ queryKey: getGamesQueryKey() })
			},
		})
	}

	delete(id?: string): CreateMutationOptions<void, HttpErrorResponse, string> {
		return mutationOptions({
			mutationKey: ['games', 'delete'],
			scope: id ? { id: id } : undefined,

			mutationFn: gameId => toPromise(this.#api.deleteGame(gameId)),
			onSuccess: async (data, gameId) => {
				await Promise.all([
					this.#client.cancelQueries({ queryKey: getGameQueryKey(gameId) }),
					this.#client.cancelQueries({ queryKey: getGamesQueryKey() }),
				])
				this.#client.removeQueries({ queryKey: getGameQueryKey(gameId), exact: true })
				this.#client.setQueriesData<PaginatedResultOfGame>({ queryKey: getGamesQueryKey() }, page => {
					if (!page || !page.items.some(game => game.id === gameId)) return page

					return {
						...page,
						items: page.items.filter(game => game.id !== gameId),
						total: Math.max(0, page.total - 1),
					}
				})
				await this.#client.invalidateQueries({ queryKey: getGamesQueryKey() })
			},
		})
	}
}
