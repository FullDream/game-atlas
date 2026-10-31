import { Component, inject, type Signal } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound, matAddRound } from '@ng-icons/material-symbols/round'
import { ROUTER_OUTLET_DATA } from '@angular/router'
import { type FeatureEditorContext } from '../feature-editor-context'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'

@Component({
	selector: 'games-maps',
	imports: [NgIcon, Button, MediaThumbnail],
	templateUrl: './game-maps.html',
	providers: [provideIcons({ matMapRound, matAddRound })],
	host: {
		role: 'region',
		'aria-label': 'Game maps',
		class: 'min-w-0 rounded-xl border-border bg-card p-4 text-card-foreground md:p-6 block border',
	},
})
export class GameMaps {
	protected readonly context = inject(ROUTER_OUTLET_DATA) as Signal<FeatureEditorContext>
}
