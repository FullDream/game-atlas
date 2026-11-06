import { Component, computed, effect, inject, untracked } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matArrowForwardRound } from '@ng-icons/material-symbols/round'
import { HlmButton, provideBrnButtonConfig } from '@spartan-ng/helm/button'
import { hlm } from '@spartan-ng/helm/utils'
import { HlmCarousel } from './hlm-carousel'

@Component({
	selector: 'button[hlmCarouselNext]',
	exportAs: 'hlmCarouselNext',
	imports: [NgIcon],
	providers: [
		provideIcons({ matArrowForwardRound }),
		provideBrnButtonConfig({ variant: 'outline', size: 'icon-sm' }),
	],
	hostDirectives: [{ directive: HlmButton, inputs: ['variant', 'size'] }],
	host: {
		'data-slot': 'carousel-next',
		'[disabled]': 'isDisabled()',
		'(click)': 'carousel.scrollNext()',
	},
	template: `
		<ng-icon name="matArrowForwardRound" class="rtl:rotate-180" />
		<span class="sr-only">Next slide</span>
	`,
})
export class HlmCarouselNext {
	protected readonly carousel = inject(HlmCarousel)

	readonly #button = inject(HlmButton)

	protected readonly isDisabled = (): boolean => !this.carousel.canScrollNext()

	readonly #computedClass = computed(() =>
		hlm(
			'h-8 w-8 absolute rounded-full',
			this.carousel.orientation() === 'horizontal'
				? '-end-12 top-1/2 -translate-y-1/2'
				: '-bottom-12 left-1/2 -translate-x-1/2 rotate-90',
		),
	)

	constructor() {
		effect(() => {
			const computedClass = this.#computedClass()

			untracked(() => this.#button.setClass(computedClass))
		})
	}
}
