import { Component, computed, effect, inject, untracked } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matArrowBackRound } from '@ng-icons/material-symbols/round'
import { HlmButton, provideBrnButtonConfig } from '@spartan-ng/helm/button'
import { hlm } from '@spartan-ng/helm/utils'
import { HlmCarousel } from './hlm-carousel'

@Component({
	selector: 'button[hlmCarouselPrevious]',
	exportAs: 'hlmCarouselPrevious',
	imports: [NgIcon],
	providers: [provideIcons({ matArrowBackRound }), provideBrnButtonConfig({ variant: 'outline', size: 'icon-sm' })],
	hostDirectives: [{ directive: HlmButton, inputs: ['variant', 'size'] }],
	host: {
		'data-slot': 'carousel-previous',
		'[disabled]': 'isDisabled()',
		'(click)': 'carousel.scrollPrev()',
	},
	template: `
		<ng-icon name="matArrowBackRound" class="rtl:rotate-180" />
		<span class="sr-only">Previous slide</span>
	`,
})
export class HlmCarouselPrevious {
	protected readonly carousel = inject(HlmCarousel)

	readonly #button = inject(HlmButton)

	protected readonly isDisabled = (): boolean => !this.carousel.canScrollPrev()

	readonly #computedClass = computed(() =>
		hlm(
			'h-8 w-8 absolute rounded-full',
			this.carousel.orientation() === 'horizontal'
				? '-start-12 top-1/2 -translate-y-1/2'
				: '-top-12 left-1/2 -translate-x-1/2 rotate-90',
		),
	)

	constructor() {
		effect(() => {
			const computedClass = this.#computedClass()

			untracked(() => this.#button.setClass(computedClass))
		})
	}
}
