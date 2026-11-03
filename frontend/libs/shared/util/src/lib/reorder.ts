import { findIndex, splice } from 'remeda'

export const reorder = <T extends { id: string }>(items: readonly T[], id: string, beforeId: string | null): T[] => {
	const from = findIndex(items, item => item.id === id)

	if (from === -1 || id === beforeId) {
		return [...items]
	}

	const to = beforeId === null ? items.length : findIndex(items, item => item.id === beforeId)

	if (to === -1) {
		return [...items]
	}

	const without = splice(items, from, 1)

	return splice(without, from < to ? to - 1 : to, 0, [items[from]])
}
