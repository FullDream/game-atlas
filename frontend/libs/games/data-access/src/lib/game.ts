export interface Game {
	readonly id: string
	readonly slug: string
	readonly title: string
	readonly description?: string
	readonly coverUrl?: string
	readonly mapsCount?: number
}

const uuidV4Pattern = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/
const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

function isGame(value: unknown): value is Game {
	if (typeof value !== 'object' || value === null || Array.isArray(value)) return false

	const game = value as Record<string, unknown>

	if (
		typeof game['id'] !== 'string' ||
		!uuidV4Pattern.test(game['id']) ||
		typeof game['slug'] !== 'string' ||
		!slugPattern.test(game['slug']) ||
		typeof game['title'] !== 'string' ||
		game['title'].trim().length === 0
	) {
		return false
	}

	if (game['description'] !== undefined && typeof game['description'] !== 'string') {
		return false
	}

	if (
		game['coverUrl'] !== undefined &&
		(typeof game['coverUrl'] !== 'string' ||
			!/^\/(?!\/)[a-zA-Z0-9/_.-]+$/.test(game['coverUrl']) ||
			game['coverUrl'].includes('..'))
	) {
		return false
	}

	if (
		game['mapsCount'] !== undefined &&
		(typeof game['mapsCount'] !== 'number' || !Number.isSafeInteger(game['mapsCount']) || game['mapsCount'] < 0)
	) {
		return false
	}

	return true
}

export function parseGames(value: unknown): readonly Game[] {
	if (!Array.isArray(value) || !value.every(isGame)) {
		throw new Error('The published game catalog is invalid.')
	}

	const ids = new Set<string>()
	const slugs = new Set<string>()

	for (const game of value) {
		if (ids.has(game.id) || slugs.has(game.slug)) {
			throw new Error('The published game catalog contains duplicate identifiers or slugs.')
		}

		ids.add(game.id)
		slugs.add(game.slug)
	}

	return value
}
