import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { filter, flatMap, isArray, isPlainObject, isString, pipe, values } from 'remeda'

export function getMediaErrorMessage(error: unknown): string {
	if (!(error instanceof HttpErrorResponse)) {
		return 'Something went wrong. Please try again.'
	}

	const { errors, detail } = isPlainObject(error.error) ? error.error : {}
	const messages = pipe(
		isPlainObject(errors) ? errors : {},
		values(),
		filter(isArray),
		flatMap(messages => messages),
		filter(isString),
	)

	if (messages.length) {
		return messages.join(' ')
	}

	if (isString(detail)) {
		return detail
	}

	switch (error.status) {
		case 0:
			return 'Could not reach the server. Check your connection and try again.'
		case HttpStatusCode.NotFound:
			return 'This item or destination no longer exists.'
		case HttpStatusCode.Conflict:
			return 'This file is in use, the folder is not empty, or a folder with this name already exists.'
		case HttpStatusCode.PayloadTooLarge:
			return 'The file exceeds the upload size limit (300 MiB by default).'
		case HttpStatusCode.UnprocessableEntity:
			return 'Please check the supplied values and supported file format.'
		default:
			return 'The operation failed. Please try again.'
	}
}
