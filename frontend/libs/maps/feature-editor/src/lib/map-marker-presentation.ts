import type { CategoryGroup, CreateMarkerVariables, Marker } from '@game-atlas/maps/data-access'
import type { MapMarker, MapPoint } from '@game-atlas/maps/ui'

export type MarkerMovementVariables = { id: string; mapId: string; position?: MapPoint }

export type PlacementState = {
	status: string
	variables: CreateMarkerVariables | undefined
	error: unknown
	submittedAt: number
	data: Marker | undefined
}

type MapMarkerPresentation = {
	markers: Marker[]
	groups: CategoryGroup[]
	movements: MarkerMovementVariables[]
	mapId: string
	selectedId: string
	placements: PlacementState[]
}

export function createMapMarkers({
	markers,
	groups,
	movements,
	mapId,
	selectedId,
	placements,
}: MapMarkerPresentation): MapMarker[] {
	const categories = groups.flatMap(group => group.categories)
	const present = (id: string, name: string, position: MapPoint, categoryId: string): MapMarker => {
		const category = categories.find(category => category.id === categoryId)

		return {
			id,
			name,
			position,
			color: category ? category.color : '#71717a',
			icon: category?.icon?.url ?? null,
		}
	}

	const persistedMarkers = markers.map(marker => {
		const moving = movements.find(state => state.mapId === mapId && state.id === marker.id)

		return {
			...present(marker.id, marker.name, moving?.position ?? marker.position, marker.categoryId),
			selected: marker.id === selectedId,
			pending: !!moving,
		}
	})

	return [
		...persistedMarkers,
		...placements.flatMap(state => {
			const request = state.variables

			if (!request?.position) return []

			return [
				{
					...present(`placement:${state.submittedAt}`, request.name, request.position, request.categoryId),
					pending: true,
					failed: state.status === 'error',
				},
			]
		}),
	]
}
