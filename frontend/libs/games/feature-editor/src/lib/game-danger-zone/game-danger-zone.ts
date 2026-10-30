import { Component, inject, type Signal } from '@angular/core'
import { ROUTER_OUTLET_DATA } from '@angular/router'
import { provideIcons } from '@ng-icons/core'
import { matDeleteRound } from '@ng-icons/material-symbols/round'
import { Button } from '@game-atlas/shared/ui'
import { type FeatureEditorContext } from '../feature-editor-context'

@Component({
	selector: 'games-danger-zone',
	imports: [Button],
	templateUrl: './game-danger-zone.html',
	providers: [provideIcons({ matDeleteRound })],
	host: {
		role: 'region',
		'aria-label': 'Danger zone',
		class: 'min-w-0 rounded-xl border-destructive/30 bg-card p-4 text-card-foreground md:p-6 block border',
	},
})
export class GameDangerZone {
	protected readonly context = inject(ROUTER_OUTLET_DATA) as Signal<FeatureEditorContext>
}
