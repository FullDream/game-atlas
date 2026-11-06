import { omit } from 'remeda'
import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import {
	type CreateMutationOptions,
	type CreateQueryOptions,
	mutationOptions,
	queryOptions,
} from '@tanstack/angular-query-experimental'
import { toPromise } from '@game-atlas/shared/util'
import { MarkersService } from '../generated/markers/markers.service'
import type { Marker } from '../generated/models'
import { getGroupsQueryKey } from '../categories/category-query-keys'
import { getMarkersQueryKey, type MarkersQueryKey } from './marker-query-keys'
import type { CreateMarkerVariables, UpdateMarkerVariables, DeleteMarkerVariables } from './markers.variables'

function upsert(markers: Marker[] | undefined, marker: Marker): Marker[] | undefined {
	return (
		markers &&
		[...markers.filter(item => item.id !== marker.id), marker].sort(
			(a, b) => a.createdAt.localeCompare(b.createdAt) || a.id.localeCompare(b.id),
		)
	)
}

@Service()
export class MarkersQueryOptions {
	readonly #service = inject(MarkersService)

	getList(gameId: string, mapId: string): CreateQueryOptions<Marker[], HttpErrorResponse, Marker[], MarkersQueryKey> {
		return queryOptions({
			queryKey: getMarkersQueryKey(gameId, mapId),
			queryFn: ({ signal }) => toPromise(this.#service.listMarkers(gameId, mapId), signal),
			staleTime: 30_000,
			enabled: Boolean(gameId && mapId),
		})
	}

	create(): CreateMutationOptions<Marker, HttpErrorResponse, CreateMarkerVariables> {
		return mutationOptions({
			mutationKey: ['markers', 'create'],
			retry: false,
			mutationFn: ({ gameId, mapId, ...payload }) =>
				toPromise(this.#service.createMarker(gameId, mapId, omit(payload, ['requestGroup']))),
			onSuccess: async (marker, { gameId, mapId }, snapshot, { client }) => {
				const queryKey = getMarkersQueryKey(gameId, mapId)

				await client.cancelQueries({ queryKey, exact: true })
				client.setQueryData<Marker[]>(queryKey, markers => upsert(markers, marker))
				await client.invalidateQueries({ queryKey: getGroupsQueryKey(gameId, mapId), exact: true })
			},
		})
	}

	update(id: string): CreateMutationOptions<Marker, HttpErrorResponse, UpdateMarkerVariables> {
		return mutationOptions({
			mutationKey: ['markers', 'update', id],
			scope: { id: `marker:${id}` },
			retry: false,
			mutationFn: ({ gameId, mapId, id, ...payload }) =>
				toPromise(this.#service.updateMarker(gameId, mapId, id, payload)),
			onSuccess: async (marker, { gameId, mapId, categoryId }, snapshot, { client }) => {
				const queryKey = getMarkersQueryKey(gameId, mapId)

				await client.cancelQueries({ queryKey, exact: true })
				client.setQueryData<Marker[]>(queryKey, markers => upsert(markers, marker))
				if (categoryId !== undefined)
					await client.invalidateQueries({ queryKey: getGroupsQueryKey(gameId, mapId), exact: true })
			},
		})
	}

	delete(id: string): CreateMutationOptions<void, HttpErrorResponse, DeleteMarkerVariables> {
		return mutationOptions({
			mutationKey: ['markers', 'delete', id],
			scope: { id: `marker:${id}` },
			retry: false,
			mutationFn: ({ gameId, mapId, id }) => toPromise(this.#service.deleteMarker(gameId, mapId, id)),
			onSuccess: async (data, { gameId, mapId, id }, snapshot, { client }) => {
				const queryKey = getMarkersQueryKey(gameId, mapId)

				await client.cancelQueries({ queryKey, exact: true })
				client.setQueryData<Marker[]>(queryKey, markers => markers?.filter(marker => marker.id !== id))
				await client.invalidateQueries({ queryKey: getGroupsQueryKey(gameId, mapId), exact: true })
			},
		})
	}
}
