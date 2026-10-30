import { type FieldTree } from '@angular/forms/signals'
import { type ImageAsset } from '@game-atlas/games/data-access'

import { type GameForm } from '@game-atlas/games/ui'

export type FeatureEditorContext = {
	fields: FieldTree<GameForm>
	cover: ImageAsset | undefined
	editing: boolean
	busy: boolean
	deleting: boolean
	gameName: string
	chooseCover: () => void
	changeStatus: (value: unknown) => void
	deleteGame: () => Promise<void>
}
