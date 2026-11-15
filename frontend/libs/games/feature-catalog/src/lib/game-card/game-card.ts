import { booleanAttribute, Component, computed, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { type Game } from '@game-atlas/games/data-access'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matArrowForwardRound, matSportsEsportsRound } from '@ng-icons/material-symbols/round'
import { HlmCardImports } from '@spartan-ng/helm/card'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'

@Component({
	selector: 'games-catalog-card',
	imports: [RouterLink, Button, MediaThumbnail, NgIcon, HlmCardImports, HlmSkeleton],
	templateUrl: './game-card.html',
	providers: [provideIcons({ matArrowForwardRound, matSportsEsportsRound })],
	host: {
		class: 'group/catalog-card block min-w-0',
		'[attr.role]': "skeleton() ? null : 'article'",
		'[attr.aria-hidden]': 'skeleton() ? true : null',
	},
})
export class GameCard {
	readonly game = input<Game>()
	readonly skeleton = input(false, { transform: booleanAttribute })

	protected readonly metadataLabel = computed(() => {
		const current = this.game()

		if (!current) return ''

		const mapsLabel = `${current.maps.length} ${current.maps.length === 1 ? 'map' : 'maps'}`
		const markersLabel = `${current.markersCount} ${current.markersCount === 1 ? 'marker' : 'markers'}`

		return `${mapsLabel} · ${markersLabel}`
	})
}
