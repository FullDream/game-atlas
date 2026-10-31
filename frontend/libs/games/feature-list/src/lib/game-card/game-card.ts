import { booleanAttribute, Component, computed, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { type Game } from '@game-atlas/games/data-access'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matArrowForwardRound,
	matEditRound,
	matFolderRound,
	matLocationOnRound,
	matMapRound,
	matMoreVertRound,
	matSportsEsportsRound,
} from '@ng-icons/material-symbols/round'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'

@Component({
	selector: 'games-card',
	imports: [RouterLink, Button, MediaThumbnail, NgIcon, HlmDropdownMenuImports, HlmSkeleton, HlmEmptyImports],
	templateUrl: './game-card.html',
	styleUrl: './game-card.css',
	providers: [
		provideIcons({
			matArrowForwardRound,
			matEditRound,
			matFolderRound,
			matLocationOnRound,
			matMapRound,
			matMoreVertRound,
			matSportsEsportsRound,
		}),
	],
	host: {
		role: 'article',
		class: 'game-card group/game-card block min-w-0 overflow-hidden rounded-xl border border-border bg-card text-card-foreground @[60rem]/games:data-[layout=list]:grid @[60rem]/games:data-[layout=list]:grid-cols-[12rem_minmax(0,1fr)_20rem] @[60rem]/games:data-[layout=list]:items-center',
		'[attr.data-layout]': 'layout()',
		'[class.list-layout]': "layout() === 'list'",
		'[attr.aria-hidden]': 'skeleton() ? true : null',
	},
})
export class GameCard {
	readonly game = input<Game>()
	readonly skeleton = input(false, { transform: booleanAttribute })
	readonly layout = input<'grid' | 'list'>('grid')

	protected readonly mapPreviews = [0, 1, 2]

	protected readonly metrics = computed(() => [
		{ icon: 'matMapRound', label: 'maps', value: this.game()?.maps.length ?? 0 },
		{ icon: 'matLocationOnRound', label: 'points', value: this.game()?.markersCount ?? 0 },
		{ icon: 'matFolderRound', label: 'categories', value: this.game()?.categoriesCount ?? 0 },
	])
}
