import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import {
	type QueryClient,
	queryOptions,
	type CreateMutationOptions,
	type CreateQueryOptions,
} from '@tanstack/angular-query-experimental'
import { toPromise } from '@game-atlas/shared/util'
import { MapsService } from './generated/maps/maps.service'
import type {
	AddMapStyleRequest,
	CreateMapRequest,
	Map as GameMap,
	MapStyle,
	UpdateMapStyleRequest,
	UpdateMapRequest,
} from './generated/models'
import { mapQueryKeys } from './map-query-keys'
import { acceptMap, acceptStyle, withStyle } from './map-cache'

type AddStyleVariables = { mapId: string; data: AddMapStyleRequest }
type RetryStyleVariables = { mapId: string; styleId: string }
type UpdateMapVariables = { id: string; patch: UpdateMapRequest }
type RenameSnapshot = { previous: GameMap | undefined; optimistic: GameMap | undefined }
type UpdateStyleVariables = { styleId: string; patch: UpdateMapStyleRequest }
type StyleSnapshot = {
	previous: MapStyle | undefined
	optimistic: MapStyle | undefined
	listPrevious: MapStyle | undefined
	listOptimistic: MapStyle | undefined
}

@Service()
export class MapsQueryOptions {
	readonly #api = inject(MapsService)

	getList(gameId: string): CreateQueryOptions<GameMap[], HttpErrorResponse> {
		return queryOptions<GameMap[], HttpErrorResponse>({
			queryKey: mapQueryKeys.list(gameId),
			staleTime: 30_000,
			queryFn: ({ signal }) => toPromise(this.#api.listMaps(gameId), signal),
		})
	}

	getDetail(gameId: string, mapId: string): CreateQueryOptions<GameMap, HttpErrorResponse> {
		return queryOptions<GameMap, HttpErrorResponse>({
			queryKey: mapQueryKeys.detail(gameId, mapId),
			refetchOnWindowFocus: false,
			refetchOnReconnect: false,
			retry: false,
			queryFn: ({ signal }) => toPromise(this.#api.getMap(gameId, mapId), signal),
		})
	}

	create(gameId: string): CreateMutationOptions<GameMap, HttpErrorResponse, CreateMapRequest> {
		return {
			mutationKey: ['maps', gameId, 'create'],
			mutationFn: data => toPromise(this.#api.createMap(gameId, data)),
			onSuccess: (map, variables, snapshot, { client }) => this.#acceptMap(client, gameId, map),
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
			onMutate: async ({ id, patch }, { client }) => {
				await client.cancelQueries({ queryKey: ['maps', gameId] })

				const key = mapQueryKeys.detail(gameId, id)

				const previous =
					client.getQueryData<GameMap>(key) ??
					client.getQueryData<GameMap[]>(mapQueryKeys.list(gameId))?.find(map => map.id === id)

				const optimistic = previous
					? {
							...previous,
							name: patch.name?.trim() ?? previous.name,
							defaultStyleId: patch.defaultStyleId ?? previous.defaultStyleId,
							styles: patch.styleOrder
								? patch.styleOrder.flatMap(id => previous.styles.filter(style => style.id === id))
								: previous.styles,
						}
					: undefined

				if (optimistic) this.#writeMap(client, gameId, optimistic)

				return { previous, optimistic: client.getQueryData<GameMap>(key) }
			},
			onError: (error, { id }, snapshot, { client }) => {
				const current = client.getQueryData<GameMap>(mapQueryKeys.detail(gameId, id))

				if (!current || !snapshot?.previous || !snapshot.optimistic) return

				const previous = snapshot.previous
				const optimistic = snapshot.optimistic
				const sameOrder =
					current.styles.map(style => style.id).join() === optimistic.styles.map(style => style.id).join()
				const styles = sameOrder
					? [...current.styles].sort(
							(a, b) =>
								previous.styles.findIndex(style => style.id === a.id) -
								previous.styles.findIndex(style => style.id === b.id),
						)
					: current.styles

				return this.#writeMap(client, gameId, {
					...current,
					styles,
					name: current.name === optimistic.name ? previous.name : current.name,
					defaultStyleId:
						current.defaultStyleId === optimistic.defaultStyleId
							? previous.defaultStyleId
							: current.defaultStyleId,
				})
			},
			onSuccess: (map, variables, snapshot, { client }) => {
				return this.#acceptMap(client, gameId, map)
			},
		}
	}

	delete(gameId: string, mapId: string): CreateMutationOptions<void, HttpErrorResponse, string> {
		return {
			mutationKey: ['maps', gameId, 'delete'],
			scope: { id: `map:${mapId}` },
			mutationFn: id => toPromise(this.#api.deleteMap(gameId, id)),
			onSuccess: async (data, id, snapshot, { client }) => {
				await client.cancelQueries({ queryKey: ['maps', gameId] })
				client.removeQueries({ queryKey: mapQueryKeys.detail(gameId, id), exact: true })
				client.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), maps => maps?.filter(map => map.id !== id))
			},
		}
	}

	addStyle(gameId: string, mapId: string): CreateMutationOptions<GameMap, HttpErrorResponse, AddStyleVariables> {
		return {
			mutationKey: ['maps', gameId, 'add-style'],
			scope: { id: `map:${mapId}` },
			mutationFn: variables => toPromise(this.#api.addMapStyle(gameId, variables.mapId, variables.data)),
			onSuccess: (map, variables, snapshot, { client }) => this.#acceptMap(client, gameId, map),
		}
	}

	updateStyle(
		gameId: string,
		mapId: string,
	): CreateMutationOptions<MapStyle, HttpErrorResponse, UpdateStyleVariables, StyleSnapshot> {
		return {
			mutationKey: ['maps', gameId, 'update-style'],
			scope: { id: `map:${mapId}` },
			mutationFn: ({ styleId, patch }) => toPromise(this.#api.updateMapStyle(gameId, mapId, styleId, patch)),
			onMutate: async ({ styleId, patch }, { client }) => {
				await client.cancelQueries({ queryKey: ['maps', gameId] })

				const listPrevious = client
					.getQueryData<GameMap[]>(mapQueryKeys.list(gameId))
					?.find(item => item.id === mapId)
					?.styles.find(style => style.id === styleId)

				const map =
					client.getQueryData<GameMap>(mapQueryKeys.detail(gameId, mapId)) ??
					client.getQueryData<GameMap[]>(mapQueryKeys.list(gameId))?.find(item => item.id === mapId)

				const previous = map?.styles.find(style => style.id === styleId)
				const optimistic = previous ? { ...previous, ...patch } : undefined

				if (optimistic) this.#changeStyle(client, gameId, mapId, styleId, () => optimistic)

				return {
					previous,
					optimistic: client
						.getQueryData<GameMap>(mapQueryKeys.detail(gameId, mapId))
						?.styles.find(style => style.id === styleId),
					listPrevious,
					listOptimistic: client
						.getQueryData<GameMap[]>(mapQueryKeys.list(gameId))
						?.find(item => item.id === mapId)
						?.styles.find(style => style.id === styleId),
				}
			},
			onError: (error, { styleId }, snapshot, { client }) => {
				const previous = snapshot?.previous

				if (!previous) return

				this.#changeStyle(client, gameId, mapId, styleId, current => {
					const optimistic = snapshot?.optimistic

					return {
						...current,
						name: current.name === optimistic?.name ? previous.name : current.name,
						backgroundColor:
							current.backgroundColor === optimistic?.backgroundColor
								? previous.backgroundColor
								: current.backgroundColor,
					}
				})
			},
			onSuccess: async (style, { styleId }, snapshot, { client }) => {
				await client.cancelQueries({ queryKey: ['maps', gameId] })

				return this.#changeStyle(client, gameId, mapId, styleId, current => acceptStyle(current, style))
			},
		}
	}

	deleteStyle(gameId: string, mapId: string): CreateMutationOptions<void, HttpErrorResponse, string> {
		return {
			mutationKey: ['maps', gameId, 'delete-style'],
			scope: { id: `map:${mapId}` },
			mutationFn: styleId => toPromise(this.#api.deleteMapStyle(gameId, mapId, styleId)),
			onSuccess: async (data, styleId, snapshot, { client }) => {
				await client.cancelQueries({ queryKey: ['maps', gameId] })

				return this.#changeStyle(client, gameId, mapId, styleId, () => undefined)
			},
		}
	}

	retryStyle(gameId: string, mapId: string): CreateMutationOptions<MapStyle, HttpErrorResponse, RetryStyleVariables> {
		return {
			mutationKey: ['maps', gameId, 'retry-style'],
			scope: { id: `map:${mapId}` },
			mutationFn: variables => toPromise(this.#api.retryMapStyle(gameId, variables.mapId, variables.styleId)),
			onSuccess: async (style, variables, snapshot, { client }) => {
				const key = mapQueryKeys.detail(gameId, variables.mapId)

				await client.cancelQueries({ queryKey: key, exact: true })

				return this.#changeStyle(client, gameId, variables.mapId, variables.styleId, current =>
					acceptStyle(current, style),
				)
			},
			onError: (error, variables, snapshot, { client }) =>
				error.status === HttpStatusCode.Conflict
					? client.invalidateQueries({ queryKey: mapQueryKeys.detail(gameId, variables.mapId), exact: true })
					: undefined,
		}
	}

	#changeStyle(
		client: QueryClient,
		gameId: string,
		mapId: string,
		styleId: string,
		change: (style: MapStyle) => MapStyle | undefined,
	): GameMap | undefined {
		const update = (map: GameMap): GameMap => withStyle(map, styleId, change)
		const result = client.setQueryData<GameMap>(mapQueryKeys.detail(gameId, mapId), map =>
			map ? update(map) : map,
		)

		client.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), maps =>
			maps?.map(map => (map.id === mapId ? update(map) : map)),
		)

		return result
	}

	async #acceptMap(client: QueryClient, gameId: string, map: GameMap): Promise<GameMap> {
		await client.cancelQueries({ queryKey: ['maps', gameId] })

		return this.#writeMap(
			client,
			gameId,
			acceptMap(client.getQueryData<GameMap>(mapQueryKeys.detail(gameId, map.id)), map),
		)
	}

	#writeMap(client: QueryClient, gameId: string, map: GameMap): GameMap {
		client.setQueryData(mapQueryKeys.detail(gameId, map.id), map)

		client.setQueryData<GameMap[]>(mapQueryKeys.list(gameId), previous => {
			if (!previous) return previous

			const maps = previous.filter(item => item.id !== map.id).concat(map)

			return maps.sort(
				(left, right) => left.createdAt.localeCompare(right.createdAt) || left.id.localeCompare(right.id),
			)
		})

		return map
	}
}
