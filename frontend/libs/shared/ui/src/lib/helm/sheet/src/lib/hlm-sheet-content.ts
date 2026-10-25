import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, effect, ElementRef, inject, input, Renderer2, signal } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCloseRound } from '@ng-icons/material-symbols/round'
import { injectExposedSideProvider, injectExposesStateProvider } from '@spartan-ng/brain/core'
import { HlmButton } from '@spartan-ng/helm/button'

import { classes } from '@spartan-ng/helm/utils'
import { HlmSheetClose } from './hlm-sheet-close'

@Component({
	selector: 'hlm-sheet-content',
	exportAs: 'hlmSheetContent',
	imports: [HlmButton, HlmSheetClose, NgIcon],
	providers: [provideIcons({ matCloseRound })],
	host: {
		'data-slot': 'sheet-content',
		'[attr.data-side]': 'sideProvider.side()',
		'[attr.data-state]': 'state()',
	},
	template: `
		<ng-content />

		@if (showCloseButton()) {
			<button hlmBtn variant="ghost" size="icon-sm" class="end-4 top-4 absolute" hlmSheetClose>
				<span class="sr-only">Close</span>
				<ng-icon name="matCloseRound" />
			</button>
		}
	`,
})
export class HlmSheetContent {
	readonly #stateProvider = injectExposesStateProvider({ host: true })
	protected readonly sideProvider = injectExposedSideProvider({ host: true })
	public readonly state = this.#stateProvider.state ?? signal('closed')
	readonly #renderer = inject(Renderer2)
	readonly #element = inject(ElementRef)

	public readonly showCloseButton = input<boolean, BooleanInput>(true, { transform: booleanAttribute })

	constructor() {
		classes(() => [
			'gap-4 bg-popover text-sm text-popover-foreground shadow-lg ease-in-out data-[side=bottom]:inset-x-0 data-[side=bottom]:bottom-0 data-[side=left]:inset-y-0 data-[side=left]:left-0 data-[side=right]:inset-y-0 data-[side=right]:right-0 data-[side=top]:inset-x-0 data-[side=top]:top-0 data-[side=left]:sm:max-w-sm data-[side=right]:sm:max-w-sm fixed flex flex-col bg-clip-padding transition duration-200 data-[side=bottom]:h-auto data-[side=bottom]:border-t data-[side=left]:h-full data-[side=left]:w-3/4 data-[side=left]:border-r data-[side=right]:h-full data-[side=right]:w-3/4 data-[side=right]:border-l data-[side=top]:h-auto data-[side=top]:border-b',
			'data-open:animate-in data-closed:animate-out',
			'data-[side=top]:data-open:slide-in-from-top data-[side=top]:data-closed:slide-out-to-top',
			'data-[side=bottom]:data-open:slide-in-from-bottom data-[side=bottom]:data-closed:slide-out-to-bottom',
			'data-[side=left]:data-open:slide-in-from-left data-[side=left]:data-closed:slide-out-to-left',
			'data-[side=right]:data-open:slide-in-from-right data-[side=right]:data-closed:slide-out-to-right',
		])
		effect(() => {
			this.#renderer.setAttribute(this.#element.nativeElement, 'data-state', this.state())
		})
	}
}
