import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import {
	QueryClient,
	queryOptions,
	type CreateMutationOptions,
	type CreateQueryOptions,
} from '@tanstack/angular-query-experimental'
import { toPromise } from '@game-atlas/shared/util'
import { MapsService } from './generated/maps/maps.service'
import type { AddMapStyleRequest, CreateMapRequest, Map as GameMap, MapStyle } from './generated/models'
import { mapQueryKeys } from './map-query-keys'

type AddStyleVariables = { mapId: string; data: AddMapStyleRequest }
type RetryStyleVariables = { mapId: string; styleId: string }

@Service()
export class MapsQueryOptions {
	readonly #api = inject(MapsService)
	readonly #queries = inject(QueryClient)

	list(gameId: string): CreateQueryOptions<GameMap[], HttpErrorResponse> {
		return queryOptions<GameMap[], HttpErrorResponse>({
			queryKey: mapQueryKeys.list(gameId),
			queryFn: ({ signal }) => toPromise(this.#api.listMaps(gameId), signal),
		})
	}

	detail(gameId: string, mapId: string): CreateQueryOptions<GameMap, HttpErrorResponse> {
		return queryOptions<GameMap, HttpErrorResponse>({
			queryKey: mapQueryKeys.detail(gameId, mapId),
			queryFn: ({ signal }) => toPromise(this.#api.getMap(gameId, mapId), signal),
		})
	}

	create(gameId: string): CreateMutationOptions<GameMap, HttpErrorResponse, CreateMapRequest> {
		return {
			mutationKey: ['maps', gameId, 'create'],
			mutationFn: data => toPromise(this.#api.createMap(gameId, data)),
			onSuccess: map => this.#acceptMap(gameId, map),
		}
	}

	addStyle(gameId: string): CreateMutationOptions<GameMap, HttpErrorResponse, AddStyleVariables> {
		return {
			mutationKey: ['maps', gameId, 'add-style'],
			mutationFn: variables => toPromise(this.#api.addMapStyle(gameId, variables.mapId, variables.data)),
			onSuccess: map => this.#acceptMap(gameId, map),
		}
	}

	retryStyle(gameId: string): CreateMutationOptions<MapStyle, HttpErrorResponse, RetryStyleVariables> {
		return {
			mutationKey: ['maps', gameId, 'retry-style'],
			mutationFn: variables => toPromise(this.#api.retryMapStyle(gameId, variables.mapId, variables.styleId)),
			onSuccess: async (style, variables) => {
				const key = mapQueryKeys.detail(gameId, variables.mapId)

				await this.#queries.cancelQueries({ queryKey: key, exact: true })
				this.#queries.setQueryData<GameMap>(key, previous => {
					if (!previous) return previous

					return { ...previous, styles: previous.styles.map(item => (item.id === style.id ? style : item)) }
				})
				await this.#queries.invalidateQueries({ queryKey: mapQueryKeys.list(gameId) })
			},
		}
	}

	async #acceptMap(gameId: string, map: GameMap): Promise<void> {
		const key = mapQueryKeys.detail(gameId, map.id)

		await this.#queries.cancelQueries({ queryKey: key, exact: true })
		this.#queries.setQueryData(key, map)
		this.#queries.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), previous => {
			if (!previous) return previous

			const maps = previous.filter(item => item.id !== map.id).concat(map)

			return maps.sort(
				(left, right) => left.createdAt.localeCompare(right.createdAt) || left.id.localeCompare(right.id),
			)
		})
	}
}
