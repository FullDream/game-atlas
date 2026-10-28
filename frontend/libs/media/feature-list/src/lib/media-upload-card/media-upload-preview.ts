import { Observable } from 'rxjs'

export function mediaUploadPreview(file: File): Observable<string | null> {
	return new Observable(subscriber => {
		const supported = ['image/jpeg', 'image/png', 'image/webp', 'image/avif'].includes(file.type)
		const url = supported ? URL.createObjectURL(file) : null

		subscriber.next(url)

		return () => {
			if (url) URL.revokeObjectURL(url)
		}
	})
}
