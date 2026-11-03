import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'

export function getCategoryErrorMessage(error: unknown): string {
	if (error instanceof HttpErrorResponse) {
		if (error.status === 0) return 'Could not connect to the server. Please try again.'

		if (error.status === HttpStatusCode.Conflict)
			return 'This name is already used in the destination. Choose another name.'

		if (error.status === HttpStatusCode.NotFound)
			return 'The group, category or image no longer exists. Refresh and try again.'

		if (error.status === HttpStatusCode.UnprocessableEntity)
			return 'Check the name, color, image and destination and try again.'
	}

	return 'Could not complete the category operation. Please try again.'
}
