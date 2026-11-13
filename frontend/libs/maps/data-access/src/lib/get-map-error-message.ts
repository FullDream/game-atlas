import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'

export function getMapRetryErrorMessage(error: unknown): string {
	if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict)
		return 'This style is no longer failed. Its current status is being refreshed.'

	return getMapStyleErrorMessage(error)
}

export function getMapStyleErrorMessage(error: unknown): string {
	if (!(error instanceof HttpErrorResponse)) return 'Could not complete the style operation. Please try again.'

	if (error.status === 0) return 'Could not connect to the server. Please try again.'

	if (error.status === HttpStatusCode.Conflict) return 'A style with this name already exists in this map.'

	if (error.status === HttpStatusCode.NotFound) return 'This map, style or source image no longer exists.'

	if (error.status === HttpStatusCode.UnprocessableEntity)
		return 'Check the style name, background color and source image. Choose a supported raster image.'

	return 'Could not complete the style operation. Please try again.'
}

export function getMapErrorMessage(error: unknown): string {
	if (!(error instanceof HttpErrorResponse)) return 'Could not complete the map operation. Please try again.'

	if (error.status === 0) return 'Could not connect to the server. Please try again.'

	if (error.status === HttpStatusCode.Conflict) {
		const detail =
			error.error && typeof error.error === 'object' && 'detail' in error.error ? error.error.detail : undefined

		return typeof detail === 'string' && detail
			? detail
			: 'A map with this name or slug already exists in this game.'
	}

	if (error.status === HttpStatusCode.NotFound) return 'This game or map no longer exists.'

	if (error.status === HttpStatusCode.UnprocessableEntity)
		return 'Check the map name, slug, default style and style order and try again.'

	return 'Could not complete the map operation. Please try again.'
}
