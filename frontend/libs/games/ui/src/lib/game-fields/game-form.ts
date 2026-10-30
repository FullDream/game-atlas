import { schema, required, pattern, maxLength } from '@angular/forms/signals'

export type GameForm = { name: string; slug: string; imageAssetId: string; status: 'draft' | 'published' }

type GameFormSource = Omit<GameForm, 'imageAssetId'> & { image: { id: string } }

export function createGameFormModel(game?: GameFormSource): GameForm {
	return {
		name: game?.name ?? '',
		slug: game?.slug ?? '',
		imageAssetId: game?.image.id ?? '',
		status: game?.status ?? 'draft',
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
	required(path.imageAssetId, { message: 'Choose a cover image.' })
})
