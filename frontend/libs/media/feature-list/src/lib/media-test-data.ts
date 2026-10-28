import type { ImageAsset, MediaAsset, MediaFolder, PaginatedResultOfMediaItem } from '@game-atlas/media/data-access'

export const imageAsset: ImageAsset = {
	type: 'image',
	id: '019a1234-5678-7000-8000-000000000001',
	name: 'Forest',
	folderId: null,
	url: '/media/files/forest.webp',
	contentType: 'image/webp',
	byteLength: 2048,
	width: 800,
	height: 600,
	alt: 'A green forest',
	placeholder: null,
	createdAt: '2026-10-27T12:00:00Z',
	updatedAt: '2026-10-27T12:00:00Z',
}

export const mediaFolder: MediaFolder = {
	type: 'folder',
	id: '019a1234-5678-7000-8000-000000000002',
	name: 'Landscapes',
	parentId: null,
	itemsCount: 0,
	createdAt: '2026-10-27T12:00:00Z',
	updatedAt: '2026-10-27T12:00:00Z',
}

export const videoAsset: MediaAsset = {
	type: 'video',
	id: '019a1234-5678-7000-8000-000000000003',
	name: 'Forest walk',
	folderId: null,
	url: '/media/files/forest-walk.webm',
	contentType: 'video/webm',
	byteLength: 4096,
	preview: imageAsset,
	createdAt: '2026-10-27T12:00:00Z',
	updatedAt: '2026-10-27T12:00:00Z',
}

export const mediaListing: PaginatedResultOfMediaItem = {
	items: [mediaFolder, imageAsset, videoAsset],
	offset: 0,
	limit: 24,
	total: 3,
}
