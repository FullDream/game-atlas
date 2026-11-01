import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'

export function getMapErrorMessage(error: unknown): string {
	if (!(error instanceof HttpErrorResponse)) return 'Could not complete the map operation. Please try again.'

	if (error.status === 0) return 'Could not connect to the server. Please try again.'

	if (error.status === HttpStatusCode.Conflict) return 'A map with this name already exists in this game.'

	if (error.status === HttpStatusCode.NotFound) return 'This game or map no longer exists.'

	if (error.status === HttpStatusCode.UnprocessableEntity) return 'Check the map name and dimensions and try again.'

	return 'Could not complete the map operation. Please try again.'
}
