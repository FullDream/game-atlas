import { inject, Service } from '@angular/core'
import type { MediaPickerSource, LibraryParams, LibraryEntry } from '@game-atlas/shared/util'
import { toPromise } from '@game-atlas/shared/util'
import { MediaService } from './generated/media/media.service'
import { getMediaErrorMessage } from './get-media-error-message'
@Service()
export class MediaPickerAdapter implements MediaPickerSource {
	readonly #service = inject(MediaService)
	readonly errorMessage = getMediaErrorMessage
	list(params: LibraryParams): ReturnType<MediaPickerSource['list']> {
		return {
			queryKey: ['media', 'attachments', params],
			queryFn: async ({ signal }) => {
				const images: LibraryEntry[] = []
				let sourceOffset = 0
				let total = 0

				do {
					const page = await toPromise(
						this.#service.listMedia({ folderId: params.folderId, offset: sourceOffset, limit: 120 }),
						signal,
					)

					total = page.total
					for (const item of page.items) {
						if (item.type === 'folder' || item.type === 'image') images.push(item)
					}

					sourceOffset += 120
				} while (sourceOffset < total)

				const filtered = images.filter(image =>
					image.name.toLowerCase().includes((params.search ?? '').toLowerCase()),
				)
				const offset = params.offset ?? 0
				const limit = params.limit ?? 24

				return { items: filtered.slice(offset, offset + limit), offset, limit, total: filtered.length }
			},
		}
	}
}
