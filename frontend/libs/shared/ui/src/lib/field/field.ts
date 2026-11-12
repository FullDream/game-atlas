import { afterRenderEffect, Component, computed, contentChild, input } from '@angular/core'
import { HlmField, HlmFieldDescription, HlmFieldError, HlmFieldLabel } from '@spartan-ng/helm/field'
import { FORM_FIELD } from '@angular/forms/signals'
import { getErrorMessageFromFieldState } from './get-error-message-from-field-state'

@Component({
	selector: 'ui-field',
	templateUrl: './field.html',
	hostDirectives: [HlmField],
	imports: [HlmFieldLabel, HlmFieldDescription, HlmFieldError],
})
export class Field {
	readonly label = input('')
	readonly description = input('')

	protected readonly formField = contentChild(FORM_FIELD, { descendants: true })

	protected readonly id = computed(() => this.formField()?.element?.id || this.formField()?.state().name())

	protected readonly error = computed(() => getErrorMessageFromFieldState(this.formField()?.state()))

	constructor() {
		afterRenderEffect(() => {
			const field = this.formField()
			const element = field?.element

			if (!element) return

			if (element instanceof HTMLElement && !element.id) {
				element.id = field.state().name()
			}
		})
	}
}
