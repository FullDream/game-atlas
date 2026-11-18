import { computed, Service, Signal } from '@angular/core'
import Dexie, { EntityTable, liveQuery } from 'dexie'
import { from, startWith, switchMap } from 'rxjs'
import { toObservable, toSignal } from '@angular/core/rxjs-interop'

export type CompletedMarker = {
	id: string
	categoryId: string
	mapId: string
	gameId: string
	completedAt: string
}

export type VisibleCategory = {
	id: string
	groupId: string
	mapId: string
	gameId: string
}

export type GroupConfig = {
	id: string
	mapId: string
	gameId: string
	visible: boolean
	expanded: boolean
}

export type MapConfig = {
	id: string
	gameId: string
	hiddenCategories: boolean
	hiddenCompleted: boolean
}

export type LiveQuerySignalOptions<P, T> = {
	params: { (): P }
	querier: { (params: P): T | Promise<T> }
}

@Service()
export class MapClientDb extends Dexie {
	declare completedMarkers: EntityTable<CompletedMarker, 'id'>
	declare visibleCategories: EntityTable<VisibleCategory, 'id'>
	declare groupConfigs: EntityTable<GroupConfig, 'id'>
	declare mapConfig: EntityTable<MapConfig, 'id'>

	constructor() {
		super('ClientDb')

		this.version(1).stores({
			completedMarkers: 'id, mapId',
			visibleCategories: 'id, mapId',
			groupConfigs: 'id, mapId',
			mapConfig: 'id',
		})
	}

	liveQuerySignal<P, T>({ params, querier }: LiveQuerySignalOptions<P, T>): Signal<T>

	liveQuerySignal<P, T>({
		params,
		querier,
		initialValue,
	}: LiveQuerySignalOptions<P, T> & { initialValue: T }): Signal<T>

	liveQuerySignal<P, T>({
		params,
		querier,
		initialValue,
	}: LiveQuerySignalOptions<P, T> & { initialValue: T }): Signal<T> {
		return toSignal(
			toObservable(computed(params)).pipe(
				switchMap(value => from(liveQuery(() => querier(value))).pipe(startWith(initialValue))),
			),
			{ initialValue },
		)
	}
}
