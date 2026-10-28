import { HttpErrorResponse, HttpEventType, provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { TestBed } from '@angular/core/testing'
import { QueryClient } from '@tanstack/angular-query-experimental'
import { firstValueFrom } from 'rxjs'
import { MediaQueryOptions } from './media-query-options'
import { MutationObserver } from '@tanstack/angular-query-experimental'
import { reconcileMediaList } from './utils'
import { MediaService } from './generated/media/media.service'
import { provideMediaBaseUrl } from './generated/api.base-url'
import type { ImageAssetDto, PaginatedResultOfMediaItemDto, MediaItemDto } from './generated/models'

const image: ImageAssetDto = {
	type: 'image',
	id: 'image-id',
	name: 'Image',
	folderId: null,
	url: '/media/files/image-id',
	contentType: 'image/png',
	byteLength: 100,
	width: 10,
	height: 10,
	alt: '',
	placeholder: null,
	createdAt: '2026-01-01T00:00:00Z',
	updatedAt: '2026-01-01T00:00:00Z',
}

function listing(items: MediaItemDto[], total = items.length): PaginatedResultOfMediaItemDto {
	return { items, total, offset: 0, limit: 24 }
}

describe('Media data access', () => {
	let client: QueryClient
	let http: HttpTestingController

	beforeEach(() => {
		client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
		TestBed.configureTestingModule({
			providers: [
				provideHttpClient(),
				provideHttpClientTesting(),
				provideMediaBaseUrl('/api'),
				{ provide: QueryClient, useValue: client },
			],
		})
		http = TestBed.inject(HttpTestingController)
	})

	afterEach(() => {
		http.verify()
		client.clear()
	})

	it('normalizes folder, search and paging', () => {
		const queries = TestBed.inject(MediaQueryOptions)

		expect(queries.list().queryKey).toEqual(queries.list({ offset: 0, limit: 24, search: ' ' }).queryKey)
		expect(queries.list({ search: ' image ' }).queryKey).toEqual(queries.list({ search: 'image' }).queryKey)
		expect(queries.list({ folderId: 'folder' }).queryKey).not.toEqual(queries.list().queryKey)
	})

	it('uses generated query parameters and preserves folder context', async () => {
		const pending = client.fetchQuery(
			TestBed.inject(MediaQueryOptions).list({ folderId: 'folder', offset: 20, search: ' image ' }),
		)
		const request = http.expectOne(req => req.url === '/api/media')

		expect(request.request.params.get('folderId')).toBe('folder')
		expect(request.request.params.get('offset')).toBe('20')
		expect(request.request.params.get('search')).toBe('image')
		request.flush(listing([image]))
		expect((await pending).items).toEqual([image])
	})

	it('preserves ProblemDetails as a rejected HTTP error', async () => {
		const pending = client.fetchQuery(TestBed.inject(MediaQueryOptions).list())
		const checked = expect(pending).rejects.toBeInstanceOf(HttpErrorResponse)
		const rejectedRequest = http.expectOne(req => req.url === '/api/media')

		rejectedRequest.flush({ detail: 'Folder not found' }, { status: 404, statusText: 'Not found' })
		await checked
	})

	it('cancels the underlying query HTTP subscription', async () => {
		const options = TestBed.inject(MediaQueryOptions).list()
		const pending = client.fetchQuery(options).catch(() => undefined)
		const request = http.expectOne(req => req.url === '/api/media')

		await client.cancelQueries({ queryKey: options.queryKey })
		expect(request.cancelled).toBe(true)
		await pending
	})

	it('uploads a multipart file with observable transfer and response events', () => {
		const file = new File(['bytes'], 'image.png', { type: 'image/png' })
		const events: number[] = []

		TestBed.inject(MediaService)
			.uploadMediaAsset({ file, folderId: 'folder' }, { observe: 'events', reportProgress: true })
			.subscribe(event => events.push(event.type))
		const request = http.expectOne('/api/media/assets')

		expect(request.request.body.get('file')).toBe(file)
		expect(request.request.body.get('folderId')).toBe('folder')
		expect(request.request.reportProgress).toBe(true)
		request.event({ type: HttpEventType.UploadProgress, loaded: 5, total: 5 })
		request.flush(image)
		expect(events).toContain(HttpEventType.UploadProgress)
		expect(events).toContain(HttpEventType.Response)
	})

	it('updates image alt and embedded video previews without refetching lists', async () => {
		const queries = TestBed.inject(MediaQueryOptions)
		const key = queries.list().queryKey
		const video = { ...image, type: 'video' as const, id: 'video', preview: image }

		client.setQueryData(key, listing([image, video]))
		const invalidation = vi.spyOn(client, 'invalidateQueries')
		const pending = new MutationObserver(client, TestBed.inject(MediaQueryOptions).updateAsset()).mutate({
			id: image.id,
			patch: { alt: 'Updated' },
		})
		const request = await vi.waitFor(() => http.expectOne('/api/media/assets/image-id'))

		expect(request.request.body).toEqual({ alt: 'Updated' })
		request.flush({ ...image, alt: 'Updated' })
		await pending
		const result = client.getQueryData<PaginatedResultOfMediaItemDto>(key)

		expect(result?.items[0]).toMatchObject({ alt: 'Updated' })
		expect(result?.items[1]).toMatchObject({ preview: { alt: 'Updated' } })
		expect(invalidation).not.toHaveBeenCalled()
	})

	it('inserts in a complete cached collection with binary order and no duplicates', async () => {
		const key = TestBed.inject(MediaQueryOptions).list().queryKey
		const data = listing([image])

		client.setQueryData(key, listing([image]))
		const result = reconcileMediaList(data, {}, null, { ...image, id: 'new', name: 'Alpha' })

		client.setQueryData(key, result.data)
		expect(client.getQueryData<PaginatedResultOfMediaItemDto>(key)?.items.map(item => item.name)).toEqual([
			'Alpha',
			'Image',
		])
		expect(client.getQueryData<PaginatedResultOfMediaItemDto>(key)?.total).toBe(2)
	})

	it('invalidates incomplete and filtered affected lists without reloading unrelated folders', async () => {
		const queries = TestBed.inject(MediaQueryOptions)
		const partial = queries.list().queryKey
		const filtered = queries.list({ search: 'Image' }).queryKey
		const other = queries.list({ folderId: 'other' }).queryKey

		client.setQueryData(partial, listing([image], 25))
		client.setQueryData(filtered, listing([image]))
		client.setQueryData(other, listing([]))
		const pending = new MutationObserver(client, TestBed.inject(MediaQueryOptions).updateAsset()).mutate({
			id: image.id,
			patch: { name: 'Changed' },
		})
		const request = await vi.waitFor(() => http.expectOne('/api/media/assets/image-id'))

		request.flush({ ...image, name: 'Changed' })
		await pending
		expect(client.getQueryState(partial)?.isInvalidated).toBe(true)
		expect(client.getQueryState(filtered)?.isInvalidated).toBe(true)
		expect(client.getQueryState(other)?.isInvalidated).toBe(false)
	})

	it('removes items only after a successful deletion', async () => {
		const key = TestBed.inject(MediaQueryOptions).list().queryKey

		client.setQueryData(key, listing([image]))
		const pending = new MutationObserver(client, TestBed.inject(MediaQueryOptions).remove()).mutate(image)
		const checked = expect(pending).rejects.toBeInstanceOf(HttpErrorResponse)
		const rejectedRequest = await vi.waitFor(() => http.expectOne('/api/media/assets/image-id'))

		rejectedRequest.flush({ detail: 'In use' }, { status: 409, statusText: 'Conflict' })

		await checked
		expect(client.getQueryData<PaginatedResultOfMediaItemDto>(key)?.items).toEqual([image])
		const success = new MutationObserver(client, TestBed.inject(MediaQueryOptions).remove()).mutate(image)

		const successfulRequest = await vi.waitFor(() => http.expectOne('/api/media/assets/image-id'))

		successfulRequest.flush(null, { status: 204, statusText: 'No content' })
		await success
		expect(client.getQueryData<PaginatedResultOfMediaItemDto>(key)?.items).toEqual([])
		expect(client.getQueryData<PaginatedResultOfMediaItemDto>(key)?.total).toBe(0)
	})

	it('preserves explicit null when clearing a cover', async () => {
		const pending = firstValueFrom(TestBed.inject(MediaService).updateMediaAsset('video', { previewAssetId: null }))
		const request = http.expectOne('/api/media/assets/video')

		expect(request.request.body).toEqual({ previewAssetId: null })
		request.flush({ ...image, type: 'video', preview: null })
		await pending
	})
})
