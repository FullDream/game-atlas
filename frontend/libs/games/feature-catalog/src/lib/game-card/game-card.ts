import { booleanAttribute, Component, computed, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { type PublicGameCard } from '@game-atlas/games/data-access'
import { Button, MediaThumbnail, ImagePreview } from '@game-atlas/shared/ui'
import { provideIcons } from '@ng-icons/core'
import { matArrowForwardRound } from '@ng-icons/material-symbols/round'
import { HlmCardImports } from '@spartan-ng/helm/card'
import { formatCount } from '@game-atlas/shared/util'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'

@Component({
	selector: 'games-catalog-card',
	imports: [RouterLink, Button, MediaThumbnail, ImagePreview, HlmCardImports, HlmSkeleton],
	templateUrl: './game-card.html',
	providers: [provideIcons({ matArrowForwardRound })],
	host: {
		class: 'group/catalog-card block min-w-0',
		'[attr.role]': "skeleton() ? null : 'article'",
		'[attr.aria-hidden]': 'skeleton() ? true : null',
	},
})
export class GameCard {
	readonly game = input<PublicGameCard>()
	readonly skeleton = input(false, { transform: booleanAttribute })

	protected readonly metadataLabel = computed(() => {
		const current = this.game()

		if (!current) return ''

		const mapsLabel = formatCount(current.mapsCount, 'map')
		const markersLabel = formatCount(current.markersCount, 'marker')

		return `${mapsLabel} · ${markersLabel}`
	})
}
