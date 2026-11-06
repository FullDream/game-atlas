import { Component, computed, inject, input } from '@angular/core'
import type { ClassValue } from 'clsx'
import { HlmCarousel } from './hlm-carousel'

@Component({
	selector: 'hlm-carousel-slide-display',
	exportAs: 'hlmCarouselSlideDisplay',
	host: {
		'data-slot': 'carousel-slide-display',
	},
	template: `
		<span class="sr-only">{{ labelContent() }}</span>
		<div aria-hidden="true" [class]="slideClass()">{{ currentSlide() }} / {{ carousel.slideCount() }}</div>
	`,
})
export class HlmCarouselSlideDisplay {
	readonly slideClass = input<ClassValue>('text-muted-foreground text-sm')

	/** Screen reader only text for the slide display */
	readonly label = input<string>('Slide')

	protected readonly carousel = inject(HlmCarousel)

	protected readonly currentSlide = computed(() => this.carousel.currentSlide() + 1)

	protected readonly labelContent = computed(() => {
		const currentSlide = this.currentSlide()
		const slideCount = this.carousel.slideCount()

		return `${this.label()} ${currentSlide} of ${slideCount} is displayed`
	})
}
