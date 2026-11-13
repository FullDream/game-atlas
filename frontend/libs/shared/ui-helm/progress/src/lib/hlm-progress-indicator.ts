import { Directionality } from '@angular/cdk/bidi'
import { Directive, computed, inject, input } from '@angular/core'
import { BrnProgressIndicator, injectBrnProgress } from '@spartan-ng/brain/progress'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmProgressIndicator],hlm-progress-indicator',
	exportAs: 'hlmProgressIndicator',
	hostDirectives: [BrnProgressIndicator],
	host: {
		'data-slot': 'progress-indicator',
		'[class.animate-indeterminate]': 'indeterminate()',
		'[style.transform]': 'transform()',
	},
})
export class HlmProgressIndicator {
	readonly variant = input<'default' | 'muted' | 'destructive'>('default')

	readonly #progress = injectBrnProgress()
	readonly #directionality = inject(Directionality)
	// Offset the indicator by the unfilled remainder. In RTL the bar fills from the inline-start
	// (visually the right), so translate the opposite way to keep the fill on the correct side.
	protected readonly transform = computed(() => {
		const offset = 100 - (this.#progress.value() ?? 100)

		return `translateX(${this.#directionality.valueSignal() === 'rtl' ? '' : '-'}${offset}%)`
	})
	protected readonly indeterminate = computed(
		() => this.#progress.value() === null || this.#progress.value() === undefined,
	)

	constructor() {
		classes(() => [
			'h-full w-full flex-1 transition-transform motion-reduce:animate-none motion-reduce:transition-none',
			{
				'bg-primary': this.variant() === 'default',
				'bg-muted-foreground': this.variant() === 'muted',
				'bg-destructive': this.variant() === 'destructive',
			},
		])
	}
}
