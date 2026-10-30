import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'

export function getGameErrorMessage(error: unknown): string {
	if (!(error instanceof HttpErrorResponse)) return 'The operation failed. Please try again.'

	if (error.status === 0) return 'Could not reach the server. Check your connection and try again.'

	if (error.status === HttpStatusCode.Conflict) return 'This slug is already used by another game.'

	if (error.status === HttpStatusCode.NotFound) return 'The game or selected image no longer exists.'

	if (error.error && typeof error.error === 'object') {
		const detail = error.error.detail

		if (typeof detail === 'string') return detail
	}

	return 'Please check the supplied values and try again.'
}
