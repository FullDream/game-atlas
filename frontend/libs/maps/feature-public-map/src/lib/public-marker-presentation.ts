import type { GroupConfig, PublicCategoryGroup, PublicMarker } from '@game-atlas/maps/data-access'
import type { MapMarker, MapPoint } from '@game-atlas/maps/ui'

export type PublicMarkerPresentationParams = {
	markers: readonly PublicMarker[]
	groups: readonly PublicCategoryGroup[]
	completedIds: ReadonlySet<string>
	visibleCategoryIds: ReadonlySet<string>
	groupConfigs: ReadonlyMap<string, GroupConfig>
	hiddenCategories: boolean
	hideCompleted: boolean
	selectedId: string | null
}

export function createPublicMapMarkers({
	markers,
	groups,
	completedIds,
	visibleCategoryIds,
	groupConfigs,
	hiddenCategories,
	hideCompleted,
	selectedId,
}: PublicMarkerPresentationParams): MapMarker[] {
	const groupsById = new Map(groups.map(group => [group.id, group]))
	const categories = groups.flatMap(group => group.categories)
	const categoriesById = new Map(categories.map(category => [category.id, category]))

	const result: MapMarker[] = []

	for (const marker of markers) {
		const category = categoriesById.get(marker.categoryId)
		const group = category ? groupsById.get(category.categoryGroupId) : undefined

		// Filter out if category or its group is hidden
		if (hiddenCategories && !visibleCategoryIds.has(marker.categoryId)) continue

		if (category && groupConfigs.get(category.categoryGroupId)?.visible === false) continue

		const isCompleted = completedIds.has(marker.id)

		// Filter out if "hide completed" is active
		if (hideCompleted && isCompleted) continue

		const position: MapPoint = {
			x: marker.position.x,
			y: marker.position.y,
		}

		result.push({
			id: marker.id,
			name: marker.name,
			position,
			color: category?.color ?? group?.color ?? '#71717a',
			icon: category?.icon?.url ?? group?.icon?.url ?? null,
			selected: marker.id === selectedId,
			opacity: isCompleted ? 0.35 : 1,
		})
	}

	return result
}
