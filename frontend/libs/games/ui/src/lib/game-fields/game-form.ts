import { schema, required, pattern, maxLength } from '@angular/forms/signals'

export type GameForm = {
	name: string
	slug: string
	coverId: string
	status: 'draft' | 'published'
	bannerId: string
	releaseDate: Date | null
	developer: string
	description: string
}

type GameFormSource = {
	name: string
	slug: string
	status: 'draft' | 'published'
	cover: { id: string }
	banner: { id: string } | null
	releaseDate: string | null
	developer: string | null
	description: string | null
}

export function createGameFormModel(game?: GameFormSource): GameForm {
	return {
		name: game?.name ?? '',
		slug: game?.slug ?? '',
		coverId: game?.cover.id ?? '',
		status: game?.status ?? 'draft',
		bannerId: game?.banner?.id ?? '',
		releaseDate: game?.releaseDate ? new Date(game.releaseDate + 'T00:00:00') : null,
		developer: game?.developer ?? '',
		description: game?.description ?? '',
	}
}

export const gameFormSchema = schema<GameForm>(path => {
	required(path.name, { message: 'Name is required.' })
	pattern(path.name, /\S/, { message: 'Name must contain a non-space character.' })
	maxLength(path.name, 200, { message: 'Name must be at most 200 characters.' })
	required(path.slug, { message: 'Slug is required.' })
	maxLength(path.slug, 200, { message: 'Slug must be at most 200 characters.' })
	pattern(path.slug, /^[a-z0-9]+(-[a-z0-9]+)*$/, {
		message: 'Use lowercase letters and numbers separated by single hyphens.',
	})
	required(path.coverId, { message: 'Choose a cover image.' })
})
