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
type UpdateMapVariables = { id: string; patch: { name?: string | null } }
type RenameSnapshot = { previous: GameMap | undefined; optimistic: GameMap | undefined }

@Service()
export class MapsQueryOptions {
	readonly #api = inject(MapsService)
	readonly #queries = inject(QueryClient)

	list(gameId: string): CreateQueryOptions<GameMap[], HttpErrorResponse> {
		return queryOptions<GameMap[], HttpErrorResponse>({
			queryKey: mapQueryKeys.list(gameId),
			staleTime: 30_000,
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

	update(
		gameId: string,
		mapId: string,
	): CreateMutationOptions<GameMap, HttpErrorResponse, UpdateMapVariables, RenameSnapshot> {
		return {
			mutationKey: ['maps', gameId, 'update'],
			scope: { id: `map:${mapId}` },
			mutationFn: ({ id, patch }) => toPromise(this.#api.updateMap(gameId, id, patch)),
			onMutate: async ({ id, patch }) => {
				await this.#queries.cancelQueries({ queryKey: ['maps', gameId] })
				const key = mapQueryKeys.detail(gameId, id)
				const previous =
					this.#queries.getQueryData<GameMap>(key) ??
					this.#queries.getQueryData<GameMap[]>(mapQueryKeys.list(gameId))?.find(map => map.id === id)
				const optimistic = previous && patch.name ? { ...previous, name: patch.name.trim() } : undefined

				if (optimistic) this.#writeMap(gameId, optimistic)

				return { previous, optimistic: this.#queries.getQueryData<GameMap>(key) }
			},
			onError: (error, { id }, snapshot) => {
				if (
					snapshot?.previous &&
					this.#queries.getQueryData(mapQueryKeys.detail(gameId, id)) === snapshot.optimistic
				) {
					this.#writeMap(gameId, snapshot.previous)
				}
			},
			onSuccess: map => this.#acceptMap(gameId, map),
		}
	}

	delete(gameId: string, mapId: string): CreateMutationOptions<void, HttpErrorResponse, string> {
		return {
			mutationKey: ['maps', gameId, 'delete'],
			scope: { id: `map:${mapId}` },
			mutationFn: id => toPromise(this.#api.deleteMap(gameId, id)),
			onSuccess: async (data, id) => {
				await this.#queries.cancelQueries({ queryKey: ['maps', gameId] })
				this.#queries.removeQueries({ queryKey: mapQueryKeys.detail(gameId, id), exact: true })
				this.#queries.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), maps =>
					maps?.filter(map => map.id !== id),
				)
			},
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
		await this.#queries.cancelQueries({ queryKey: ['maps', gameId] })
		this.#writeMap(gameId, map)
	}

	#writeMap(gameId: string, map: GameMap): void {
		this.#queries.setQueryData(mapQueryKeys.detail(gameId, map.id), map)
		this.#queries.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), previous => {
			if (!previous) return previous

			const maps = previous.filter(item => item.id !== map.id).concat(map)

			return maps.sort(
				(left, right) => left.createdAt.localeCompare(right.createdAt) || left.id.localeCompare(right.id),
			)
		})
	}
}
