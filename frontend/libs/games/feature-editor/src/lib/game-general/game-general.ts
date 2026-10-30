import { Component, inject, type Signal } from '@angular/core'
import { ROUTER_OUTLET_DATA } from '@angular/router'
import { GameFields } from '@game-atlas/games/ui'
import { type FeatureEditorContext } from '../feature-editor-context'

@Component({
	selector: 'games-general',
	imports: [GameFields],
	templateUrl: './game-general.html',
	host: { class: 'contents' },
})
export class GameGeneral {
	protected readonly context = inject(ROUTER_OUTLET_DATA) as Signal<FeatureEditorContext>
}
