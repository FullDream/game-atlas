import type { ListGamesSortBy, ListGamesSortDirection } from './generated/models'

export type GameSortValue = `${ListGamesSortBy}:${ListGamesSortDirection}`

export interface GameSortOption {
	readonly value: GameSortValue
	readonly label: string
}

export const GAME_SORT_LABELS: Record<GameSortValue, string> = {
	'name:asc': 'Name A–Z',
	'name:desc': 'Name Z–A',
	'createdAt:desc': 'Newest first',
	'createdAt:asc': 'Oldest first',
	'updatedAt:desc': 'Recently updated',
	'updatedAt:asc': 'Least recently updated',
}

export const GAME_SORT_OPTIONS: GameSortOption[] = (Object.entries(GAME_SORT_LABELS) as [GameSortValue, string][]).map(
	([value, label]) => ({
		value,
		label,
	}),
)

function isGameSortValue(value: unknown): value is GameSortValue {
	return typeof value === 'string' && Object.prototype.hasOwnProperty.call(GAME_SORT_LABELS, value)
}

export function getGameSortLabel(value: unknown, fallback = 'Sort games'): string {
	return isGameSortValue(value) ? GAME_SORT_LABELS[value] : fallback
}

export function parseGameSort(
	value: unknown,
): { sortBy: ListGamesSortBy; sortDirection: ListGamesSortDirection } | null {
	if (!isGameSortValue(value)) return null

	const [sortBy, sortDirection] = value.split(':') as [ListGamesSortBy, ListGamesSortDirection]

	return { sortBy, sortDirection }
}
