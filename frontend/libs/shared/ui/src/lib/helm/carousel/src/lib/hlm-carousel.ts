import { Directionality } from '@angular/cdk/bidi'
import { Component, type InputSignal, type Signal, computed, inject, input, signal, viewChild } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import type { EmblaEventType, EmblaOptionsType, EmblaPluginType } from 'embla-carousel'
import { EmblaCarouselDirective } from 'embla-carousel-angular'

@Component({
	selector: 'hlm-carousel',
	exportAs: 'hlmCarousel',
	imports: [EmblaCarouselDirective],
	templateUrl: './hlm-carousel.html',
	host: {
		'data-slot': 'carousel',
		role: 'region',
		'aria-roledescription': 'carousel',
		'(keydown)': 'onKeydown($event)',
	},
})
export class HlmCarousel {
	readonly orientation = input<'horizontal' | 'vertical'>('horizontal')
	readonly options: InputSignal<Omit<EmblaOptionsType, 'axis'> | undefined> = input<Omit<EmblaOptionsType, 'axis'>>()
	readonly plugins: InputSignal<EmblaPluginType[]> = input<EmblaPluginType[]>([])

	readonly #dir = inject(Directionality)

	readonly #canScrollPrev = signal(false)
	readonly #canScrollNext = signal(false)
	readonly #currentSlide = signal(0)
	readonly #slideCount = signal(0)

	readonly canScrollPrev = this.#canScrollPrev.asReadonly()
	readonly canScrollNext = this.#canScrollNext.asReadonly()
	readonly currentSlide = this.#currentSlide.asReadonly()
	readonly slideCount = this.#slideCount.asReadonly()

	protected readonly emblaCarousel = viewChild.required(EmblaCarouselDirective)

	protected readonly emblaOptions: Signal<EmblaOptionsType> = computed(() => ({
		// Default embla's direction to the ambient layout direction so the carousel scrolls RTL
		// automatically; an explicit `direction` in `options` still wins.
		direction: this.#dir.valueSignal(),
		...this.options(),
		axis: this.orientation() === 'horizontal' ? 'x' : 'y',
	}))

	constructor() {
		classes(() => 'relative')
	}

	scrollPrev(): void {
		this.emblaCarousel().scrollPrev()
	}

	scrollNext(): void {
		this.emblaCarousel().scrollNext()
	}

	protected onEmblaEvent(event: EmblaEventType): void {
		const emblaApi = this.emblaCarousel().emblaApi

		if (!emblaApi) {
			return
		}

		if (event === 'select' || event === 'init' || event === 'reInit') {
			this.#canScrollPrev.set(emblaApi.canScrollPrev())
			this.#canScrollNext.set(emblaApi.canScrollNext())

			this.#currentSlide.set(emblaApi.selectedScrollSnap())
			this.#slideCount.set(emblaApi.scrollSnapList().length)
		}
	}

	protected onKeydown(event: KeyboardEvent): void {
		if (event.key === 'ArrowLeft') {
			event.preventDefault()
			this.emblaCarousel().scrollPrev()
		} else if (event.key === 'ArrowRight') {
			event.preventDefault()
			this.emblaCarousel().scrollNext()
		}
	}
}
