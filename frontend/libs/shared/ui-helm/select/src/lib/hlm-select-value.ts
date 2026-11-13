import { Directive, inject } from '@angular/core'
import { BrnSelectValue } from '@spartan-ng/brain/select'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmSelectValue],hlm-select-value',
	exportAs: 'hlmSelectValue',
	hostDirectives: [{ directive: BrnSelectValue, inputs: ['placeholder'] }],
	host: { '[attr.data-slot]': '!hidden() ? "select-value" : null' },
})
export class HlmSelectValue {
	readonly #brnSelectValue = inject(BrnSelectValue)

	protected readonly hidden = this.#brnSelectValue.hidden

	constructor() {
		classes(() => 'data-hidden:hidden')
	}
}
