import { _IdGenerator as IdGenerator } from '@angular/cdk/a11y'
import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, effect, inject, input } from '@angular/core'
import { BrnField, BrnFieldA11yService } from '@spartan-ng/brain/field'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-field-error',
	exportAs: 'hlmFieldError',
	template: '<ng-content />',
	host: {
		role: 'alert',
		'data-slot': 'field-error',
		'[attr.id]': 'id()',
		'[hidden]': '!display()',
	},
})
export class HlmFieldError {
	readonly id = input(inject(IdGenerator).getId('hlm-field-error-'))
	readonly validator = input<string>()
	readonly forceShow = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	readonly #field = inject(BrnField, { optional: true })
	readonly #a11y = inject(BrnFieldA11yService, { optional: true, host: true })

	protected readonly display = computed(() => {
		if (this.forceShow() || !this.#field) return true

		const errors = this.#field.errors()
		const validator = this.validator()

		return (
			!!this.#field.controlState()?.spartanInvalid &&
			!!errors &&
			(validator ? validator in errors : Object.keys(errors).length > 0)
		)
	})

	constructor() {
		classes(() => 'spartan-field-error font-normal')

		effect(onCleanup => {
			if (!this.#a11y || !this.display()) return

			const id = this.id()
			const a11y = this.#a11y

			a11y.registerError(id)
			onCleanup(() => a11y.unregisterError(id))
		})
	}
}
