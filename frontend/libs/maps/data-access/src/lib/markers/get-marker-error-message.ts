import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'

export function isMarkerOutcomeUncertain(error: unknown): boolean {
	return (
		!(error instanceof HttpErrorResponse) ||
		error.status === 0 ||
		error.status >= HttpStatusCode.InternalServerError
	)
}

export function getMarkerErrorMessage(error: unknown): string {
	if (error instanceof HttpErrorResponse) {
		if (error.status === HttpStatusCode.NotFound)
			return 'The marker, category or image no longer exists. Refresh and try again.'

		if (error.status === HttpStatusCode.UnprocessableEntity)
			return 'Check the name, description, category, coordinates and images.'

		if (error.status === 0) return 'Connection lost. Refresh markers to check whether the operation completed.'
	}

	return 'Could not complete the marker operation. Refresh markers before trying again.'
}
