import { HlmCarouselImports } from '@spartan-ng/helm/carousel'
import { HlmCheckbox } from '@spartan-ng/helm/checkbox'
import { HlmLabel } from '@spartan-ng/helm/label'
import { NgTemplateOutlet } from '@angular/common'
import { HlmAspectRatioImports } from '@spartan-ng/helm/aspect-ratio'
import { Button } from '@game-atlas/shared/ui'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { Component, input, output } from '@angular/core'
import type { PublicCategory, PublicMarker } from '@game-atlas/maps/data-access'
import { provideIcons } from '@ng-icons/core'
import { matCloseRound, matOpenInNewRound } from '@ng-icons/material-symbols/round'

@Component({
	selector: 'maps-marker-popup',
	imports: [Button, HlmTooltip, HlmCheckbox, HlmLabel, NgTemplateOutlet, HlmCarouselImports, HlmAspectRatioImports],
	providers: [
		provideIcons({
			matCloseRound,
			matOpenInNewRound,
		}),
	],
	templateUrl: './marker-popup.html',
})
export class MarkerPopup {
	readonly panel = input(false)
	readonly mobile = input(false)
	readonly contentOnly = input(false)
	readonly marker = input<PublicMarker | null>(null)
	readonly category = input<PublicCategory | null>(null)
	readonly isCompleted = input<boolean>(false)

	readonly closed = output<void>()
	readonly expand = output<void>()
	readonly toggleCompleted = output<void>()
}
