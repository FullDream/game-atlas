import { Component, computed, contentChild, inject, input } from '@angular/core'
import { FormField } from '@angular/forms/signals'
import { HlmFieldError } from './hlm-field-error'
import { BrnField } from '@spartan-ng/brain/field'
import { classes } from '@spartan-ng/helm/utils'
import { cva, VariantProps } from 'class-variance-authority'

const fieldVariants = cva('data-[matches-spartan-invalid=true]:text-destructive gap-3 group/field flex w-full', {
	variants: {
		orientation: {
			vertical: 'flex-col *:w-full [&>.sr-only]:w-auto',
			horizontal: [
				'flex-row items-center',
				'*:data-[slot=field-label]:flex-auto',
				'has-[>[data-slot=field-content]]:items-start has-[>[data-slot=field-content]]:[&>[role=checkbox],[role=radio]]:mt-px',
			],
			responsive: [
				'@md/field-group:flex-row @md/field-group:items-center @md/field-group:*:w-auto flex-col *:w-full [&>.sr-only]:w-auto',
				'@md/field-group:*:data-[slot=field-label]:flex-auto',
				'@md/field-group:has-[>[data-slot=field-content]]:items-start @md/field-group:has-[>[data-slot=field-content]]:[&>[role=checkbox],[role=radio]]:mt-px',
			],
		},
	},
	defaultVariants: {
		orientation: 'vertical',
	},
})

export type FieldVariants = VariantProps<typeof fieldVariants>

@Component({
	selector: 'hlm-field',
	exportAs: 'hlmField',
	imports: [HlmFieldError],
	templateUrl: './hlm-field.html',
	hostDirectives: [{ directive: BrnField, inputs: ['data-invalid', 'forceInvalid'] }],
	host: {
		role: 'group',
		'data-slot': 'field',
		'[attr.data-orientation]': 'orientation()',
		'[attr.data-invalid]': 'invalid() ? "true" : null',
	},
})
export class HlmField {
	readonly orientation = input<FieldVariants['orientation']>('vertical')

	readonly #field = inject(BrnField)
	protected readonly control = contentChild(FormField, { descendants: true })
	protected readonly customError = contentChild(HlmFieldError)

	protected readonly errors = computed(() => {
		const state = this.control()?.state()

		return state?.touched() && !this.customError() ? state.errors() : []
	})

	protected readonly invalid = computed(() => {
		const state = this.control()?.state()

		return (
			this.#field.forceInvalid() ||
			this.#field.dataInvalid() ||
			(state ? state.touched() && state.invalid() : this.#field.controlState()?.spartanInvalid)
		)
	})

	constructor() {
		classes(() => fieldVariants({ orientation: this.orientation() }))
	}
}
