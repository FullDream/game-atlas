import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { HlmCardConfig, injectHlmCardConfig } from './hlm-card.token'

@Directive({
	selector: '[hlmCard],hlm-card',
	exportAs: 'hlmCard',
	host: {
		'data-slot': 'card',
		'[attr.data-size]': 'size()',
	},
})
export class HlmCard {
	readonly #defaultConfig = injectHlmCardConfig()
	readonly size = input<HlmCardConfig['size']>(this.#defaultConfig.size)

	constructor() {
		classes(
			() =>
				'ring-foreground/10 bg-card text-card-foreground rounded-2xl text-sm [--card-spacing:--spacing(6)] has-[>img:first-child]:pt-0 data-[size=sm]:[--card-spacing:--spacing(4)] *:[img:first-child]:rounded-t-xl *:[img:last-child]:rounded-b-xl group/card flex flex-col gap-(--card-spacing) overflow-hidden py-(--card-spacing) ring-1',
		)
	}
}
