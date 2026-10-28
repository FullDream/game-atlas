import { _IdGenerator as IdGenerator } from '@angular/cdk/a11y'
import { Directive, effect, inject, input } from '@angular/core'
import { BrnFieldA11yService } from '@spartan-ng/brain/field'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmFieldDescription],hlm-field-description',
	exportAs: 'hlmFieldDescription',
	host: { 'data-slot': 'field-description', '[attr.id]': 'id()' },
})
export class HlmFieldDescription {
	readonly id = input(inject(IdGenerator).getId('hlm-field-description-'))

	readonly #a11y = inject(BrnFieldA11yService, { optional: true, host: true })

	constructor() {
		classes(() => [
			'spartan-field-description leading-normal font-normal group-has-data-horizontal/field:text-balance',
			'last:mt-0 nth-last-2:-mt-1',
			'[&>a:hover]:text-primary [&>a]:underline [&>a]:underline-offset-4',
		])

		effect(onCleanup => {
			if (!this.#a11y) return

			const id = this.id()
			const a11y = this.#a11y

			a11y.registerDescription(id)
			onCleanup(() => a11y.unregisterDescription(id))
		})
	}
}
