import type { CategoryGroup, CreateMarkerVariables, Marker, PublicationStatus } from '@game-atlas/maps/data-access'
import type { MapMarker, MapPoint, MarkerDraftState } from '@game-atlas/maps/ui'

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
	const groupsById = new Map(groups.map(group => [group.id, group]))
	const categories = groups.flatMap(group => group.categories)
	const present = (
		id: string,
		name: string,
		position: MapPoint,
		categoryId: string,
		markerStatus: PublicationStatus | undefined,
	): MapMarker => {
		const category = categories.find(category => category.id === categoryId)
		const group = category ? groupsById.get(category.categoryGroupId) : undefined
		const draftStates: MarkerDraftState[] = []

		if (markerStatus === 'draft') draftStates.push('marker')

		if (category?.status === 'draft') draftStates.push('category')

		if (group?.status === 'draft') draftStates.push('group')

		return {
			id,
			name,
			position,
			color: category ? category.color : '#71717a',
			icon: category?.icon?.url ?? null,
			...(draftStates.length ? { draftStates } : {}),
		}
	}

	const persistedMarkers = markers.map(marker => {
		const moving = movements.find(state => state.mapId === mapId && state.id === marker.id)

		return {
			...present(marker.id, marker.name, moving?.position ?? marker.position, marker.categoryId, marker.status),
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
					...present(
						`placement:${state.submittedAt}`,
						request.name,
						request.position,
						request.categoryId,
						request.status ?? 'draft',
					),
					pending: true,
					failed: state.status === 'error',
				},
			]
		}),
	]
}
