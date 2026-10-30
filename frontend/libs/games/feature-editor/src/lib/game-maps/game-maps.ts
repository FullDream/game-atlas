import { Component } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound, matAddRound } from '@ng-icons/material-symbols/round'
import { Button } from '@game-atlas/shared/ui'

@Component({
	selector: 'games-maps',
	imports: [NgIcon, Button],
	templateUrl: './game-maps.html',
	providers: [provideIcons({ matMapRound, matAddRound })],
	host: {
		role: 'region',
		'aria-label': 'Game maps',
		class: 'min-w-0 rounded-xl border-border bg-card p-4 text-card-foreground md:p-6 block border',
	},
})
export class GameMaps {}
