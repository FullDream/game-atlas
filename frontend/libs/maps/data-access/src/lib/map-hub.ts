import { InjectionToken, inject, type Provider } from '@angular/core'
import { ObservableHub, provideHub } from '@game-atlas/shared/data-access'
import type { MapStyle, MapProcessingStatus, MapProcessingError } from './generated/models'

export type MapProcessingUpdate = {
	mapId: string
	styleId: string | null
	attemptId: string | null
	status: MapProcessingStatus | null
	progress: number
	error: MapProcessingError | null
	deleted: boolean
}

type MapHubMethods = { JoinMap: [mapId: string]; LeaveMap: [mapId: string] }
type MapHubEvents = { StyleUpdated: [style: MapStyle]; ProcessingUpdated: [processing: MapProcessingUpdate] }

export const MAP_HUB = new InjectionToken<ObservableHub<MapHubMethods, MapHubEvents>>('Map hub')
const MAP_HUB_URL = new InjectionToken<string>('Map hub URL')

export function provideMapHubUrl(url: string): Provider {
	return { provide: MAP_HUB_URL, useValue: url }
}

/** Component providers ensure provideHub's onDestroy belongs to the editor. */
export function provideMapHub(): Provider {
	return provideHub(MAP_HUB, () => inject(MAP_HUB_URL))
}
