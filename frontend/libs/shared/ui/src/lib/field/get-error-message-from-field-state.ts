import { FieldState, NgValidationError } from '@angular/forms/signals'
import { formatDate } from '@angular/common'

export const getErrorMessageFromFieldState = (fieldState?: FieldState<unknown>): string => {
	if (!fieldState) {
		return ''
	}

	if (fieldState.invalid() && (fieldState.touched() || fieldState.dirty() || fieldState.submitting())) {
		const error = fieldState.errors()[0]

		if (!error) {
			return ''
		}

		if (error.message) {
			return error.message
		}

		if (error instanceof NgValidationError) {
			switch (error.kind) {
				case 'required':
					return 'This field is required'
				case 'email':
					return 'Enter a valid email address'
				case 'min':
					return `Value must be at least ${error.min}`
				case 'max':
					return `Value must be at most ${error.max}`
				case 'minLength':
					return `Must be at least ${error.minLength} characters`
				case 'maxLength':
					return `Must be no more than ${error.maxLength} characters`
				case 'minDate':
					return `Date must be on or after ${formatDate(error.minDate, 'medium', 'en-US')}`
				case 'maxDate':
					return `Date must be on or before ${formatDate(error.maxDate, 'medium', 'en-US')}`
			}
		}

		return 'Please check the entered value'
	}

	return ''
}
