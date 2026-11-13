import { Directive, inject } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { HlmCarousel } from './hlm-carousel'

@Directive({
	selector: '[hlmCarouselContent],hlm-carousel-content',
	exportAs: 'hlmCarouselContent',
	host: {
		'data-slot': 'carousel-content',
	},
})
export class HlmCarouselContent {
	readonly #orientation = inject(HlmCarousel).orientation

	constructor() {
		classes(() => ['flex', this.#orientation() === 'horizontal' ? '-ml-4' : '-mt-4 flex-col'])
	}
}
