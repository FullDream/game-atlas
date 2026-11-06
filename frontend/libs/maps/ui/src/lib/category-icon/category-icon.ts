import { Component, computed, input } from '@angular/core'
import { matLocationOnFillRound } from '@ng-icons/material-symbols/round'

const fallbackIcon = `data:image/svg+xml,${encodeURIComponent(matLocationOnFillRound)}`

@Component({
	selector: 'maps-category-icon',
	template: '',
	host: {
		class: 'inline-block size-5 shrink-0',
		'aria-hidden': 'true',
		'[style.color]': 'color()',
		'[style.background-color]': 'color() ? "currentColor" : null',
		'[style.background-image]': 'color() ? null : imageUrl()',
		'[style.background-size]': '"contain"',
		'[style.background-position]': '"center"',
		'[style.background-repeat]': '"no-repeat"',
		'[style.mask-image]': 'color() ? imageUrl() : null',
		'[style.mask-mode]': '"alpha"',
		'[style.mask-size]': '"contain"',
		'[style.mask-position]': '"center"',
		'[style.mask-repeat]': '"no-repeat"',
	},
})
export class CategoryIcon {
	readonly iconUrl = input<string | null | undefined>(null)
	readonly color = input<string | null>(null)

	protected readonly imageUrl = computed(() => `url(${JSON.stringify(this.iconUrl() || fallbackIcon)})`)
}
