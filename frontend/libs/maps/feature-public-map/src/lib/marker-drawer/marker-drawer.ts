import { afterRenderEffect, Component, ElementRef, input, output, viewChild } from '@angular/core'
import type { PublicCategory, PublicMarker } from '@game-atlas/maps/data-access'
import { HlmDrawerImports } from '@spartan-ng/helm/drawer'
import { MarkerPopup } from '../marker-popup/marker-popup'
import { Button } from '@game-atlas/shared/ui'
import { HlmCheckbox } from '@spartan-ng/helm/checkbox'
import { HlmLabel } from '@spartan-ng/helm/label'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { provideIcons } from '@ng-icons/core'
import { matCloseRound, matOpenInNewOffRound } from '@ng-icons/material-symbols/round'

export type MarkerDrawerMode = 'desktop-right' | 'mobile-bottom'

@Component({
	selector: 'maps-marker-drawer',
	imports: [HlmDrawerImports, MarkerPopup, Button, HlmCheckbox, HlmLabel, HlmTooltip],
	templateUrl: './marker-drawer.html',
	providers: [provideIcons({ matCloseRound, matOpenInNewOffRound })],
})
export class MarkerDrawer {
	readonly marker = input<PublicMarker | null>(null)
	readonly category = input<PublicCategory | null>(null)
	readonly isCompleted = input(false)
	readonly mode = input<MarkerDrawerMode>('desktop-right')

	readonly closed = output<void>()
	readonly collapse = output<void>()
	readonly toggleCompleted = output<void>()
	readonly heightChanged = output<number>()
	readonly widthChanged = output<number>()

	protected readonly content = viewChild('content', { read: ElementRef<HTMLElement> })

	constructor() {
		afterRenderEffect(onCleanup => {
			const content = this.content()?.nativeElement

			if (!content) return

			const observer = new ResizeObserver(() => {
				const bounds = content.getBoundingClientRect()

				this.heightChanged.emit(bounds.height)
				this.widthChanged.emit(bounds.width)
			})

			observer.observe(content)
			onCleanup(() => observer.disconnect())
		})
	}
}
