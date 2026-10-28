import { CdkDropListGroup } from '@angular/cdk/drag-drop'
import { CdkPortalOutlet } from '@angular/cdk/portal'
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { Component, inject } from '@angular/core'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import { By } from '@angular/platform-browser'
import { provideRouter, Router, RouterOutlet, withComponentInputBinding } from '@angular/router'
import {
	MediaService,
	type ListMediaParams,
	type MediaFolderDetailsDto,
	type PaginatedResultOfMediaItemDto,
} from '@game-atlas/media/data-access'
import { PageHeaderOutlet, WorkspaceBreadcrumbs } from '@game-atlas/shared/ui'
import { toast } from '@spartan-ng/brain/sonner'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { of, Subject, throwError } from 'rxjs'
import { MediaCard } from './media-card/media-card'
import { imageAsset, mediaFolder, mediaListing } from './media-test-data'
import { getTestButton, getTestElement } from './media-test-elements'
import { MEDIA_ROUTES } from './media.routes'

@Component({
	selector: 'media-test-host',
	imports: [CdkPortalOutlet, RouterOutlet, WorkspaceBreadcrumbs],
	template: `
		<ui-workspace-breadcrumbs [items]="header.breadcrumbs()" />
		<ng-template [cdkPortalOutlet]="header.portal()" />
		<router-outlet />
	`,
	hostDirectives: [PageHeaderOutlet, CdkDropListGroup],
})
class MediaTestHost {
	protected readonly header = inject(PageHeaderOutlet)
}

@Component({ selector: 'media-other-page', template: '' })
class OtherPage {}

describe('FeatureList routing and interactions', () => {
	let fixture: ComponentFixture<MediaTestHost>
	let router: Router
	let service: {
		listMedia: ReturnType<typeof vi.fn>
		getMediaFolder: ReturnType<typeof vi.fn>
		updateMediaAsset: ReturnType<typeof vi.fn>
		deleteMediaAsset: ReturnType<typeof vi.fn>
	}

	beforeEach(() => {
		service = {
			listMedia: vi.fn((params: ListMediaParams) =>
				of({ ...mediaListing, offset: params.offset ?? 0, limit: params.limit ?? 24 }),
			),
			getMediaFolder: vi.fn(() => of({ folder: mediaFolder, ancestors: [] })),
			updateMediaAsset: vi.fn(() => of(imageAsset)),
			deleteMediaAsset: vi.fn(() => of(undefined)),
		}

		TestBed.configureTestingModule({
			providers: [
				provideRouter(
					[
						{ path: 'media', children: MEDIA_ROUTES },
						{ path: 'games', component: OtherPage },
					],
					withComponentInputBinding(),
				),
				provideSpartanHlm(),
				provideTanStackQuery(
					new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } }),
				),
				{ provide: MediaService, useValue: service },
			],
		})
		router = TestBed.inject(Router)
	})

	afterEach(() => {
		fixture?.destroy()
		TestBed.inject(QueryClient).clear()
	})

	async function render(url = '/media'): Promise<HTMLElement> {
		fixture = TestBed.createComponent(MediaTestHost)
		await router.navigateByUrl(url)
		await fixture.whenStable()
		await vi.waitFor(() => expect(fixture.nativeElement.querySelectorAll('media-card')).toHaveLength(3))

		return fixture.nativeElement
	}

	it('renders server items and header actions while preserving the disabled parent control at root', async () => {
		const element = await render()
		const parent = getTestElement<HTMLAnchorElement>('[aria-label="Up one folder"]', element)

		expect(element.textContent).toContain(imageAsset.name)
		expect(element.querySelector('[aria-label="New folder"]')).not.toBeNull()
		expect(element.querySelector('[aria-label="Upload file"]')).not.toBeNull()
		expect(parent.getAttribute('aria-disabled')).toBe('true')
		expect(parent.hasAttribute('href')).toBe(false)
		expect(service.getMediaFolder).not.toHaveBeenCalled()
	})

	it('shows a responsive two-row skeleton grid on the first request', async () => {
		const response = new Subject<PaginatedResultOfMediaItemDto>()

		service.listMedia.mockReturnValue(response)
		fixture = TestBed.createComponent(MediaTestHost)
		await router.navigateByUrl('/media')
		const element = fixture.nativeElement as HTMLElement

		await vi.waitFor(() => expect(element.querySelectorAll('media-card[data-skeleton]')).toHaveLength(24))

		expect(element.querySelector('.media-grid')?.getAttribute('data-skeleton-rows')).toBe('2')
		expect(element.querySelector('media-card a, media-card button')).toBeNull()
		expect(element.querySelector('[role="status"]')?.textContent).toContain('Loading your library')
		response.next(mediaListing)
		response.complete()
		await fixture.whenStable()

		await vi.waitFor(() => {
			expect(element.querySelector('[data-skeleton]')).toBeNull()
			expect(element.querySelectorAll('media-card')).toHaveLength(3)
		})
	})

	it.each([0, 3, 24, 25])('uses the cached folder count %s while directory requests are pending', async count => {
		const response = new Subject<PaginatedResultOfMediaItemDto>()
		const context = new Subject<MediaFolderDetailsDto>()

		service.listMedia.mockReturnValueOnce(
			of({ ...mediaListing, items: [{ ...mediaFolder, itemsCount: count }, ...mediaListing.items.slice(1)] }),
		)
		const element = await render()

		service.listMedia.mockReturnValue(response)
		service.getMediaFolder.mockReturnValue(context)
		await router.navigateByUrl(`/media?folderId=${mediaFolder.id}`)
		await vi.waitFor(() => expect(service.listMedia).toHaveBeenCalledTimes(2))
		await vi.waitFor(() => expect(element.querySelector('.media-grid')?.getAttribute('aria-busy')).toBe('true'))

		expect(element.querySelectorAll('media-card[data-skeleton]')).toHaveLength(count <= 24 ? count : 24)
		expect(element.querySelector('.media-grid')?.getAttribute('data-skeleton-rows')).toBe(count <= 24 ? null : '2')
		expect(element.querySelector('media-card a')).toBeNull()
		expect(element.textContent).not.toContain('This folder is empty')
		context.next({ folder: { ...mediaFolder, itemsCount: 0 }, ancestors: [] })
		context.complete()
		response.next({ items: [], limit: 24, offset: 0, total: 0 })
		response.complete()
		await fixture.whenStable()

		await vi.waitFor(() => {
			expect(element.querySelector('[data-skeleton]')).toBeNull()
			expect(element.textContent).toContain('This folder is empty')
		})
	})

	it.each([
		['search', '/media?search=Lake', 0, 24],
		['pagination', '/media?offset=24', 24, 24],
		['page size', '/media?limit=48', 0, 48],
	])('keeps previous cards during %s changes in the same directory', async (change, url, offset, limit) => {
		const response = new Subject<PaginatedResultOfMediaItemDto>()
		const element = await render()

		service.listMedia.mockReturnValue(response)
		await router.navigateByUrl(String(url))
		await vi.waitFor(() => expect(service.listMedia).toHaveBeenCalledTimes(2))
		await vi.waitFor(() => expect(element.querySelector('.media-grid')?.getAttribute('aria-busy')).toBe('true'))

		expect(element.querySelector('[data-skeleton]')).toBeNull()
		expect(element.querySelectorAll('media-card')).toHaveLength(3)
		expect(element.textContent).toContain(imageAsset.name)
		expect(router.url).toBe(url)
		response.next({
			items: [{ ...imageAsset, name: 'New result' }],
			offset: Number(offset),
			limit: Number(limit),
			total: 60,
		})
		response.complete()
		await fixture.whenStable()

		await vi.waitFor(() => {
			expect(element.querySelectorAll('media-card')).toHaveLength(1)
			expect(element.textContent).toContain('New result')
		})
	})

	it('forwards valid non-page-aligned offsets, limits, folder IDs and normalized search', async () => {
		service.listMedia.mockImplementation((params: ListMediaParams) =>
			of({ ...mediaListing, offset: params.offset ?? 0, limit: params.limit ?? 24, total: 100 }),
		)
		await render(`/media?folderId=${mediaFolder.id}&offset=7&limit=48&search=Forest`)

		expect(service.listMedia).toHaveBeenCalledExactlyOnceWith({
			folderId: mediaFolder.id,
			offset: 7,
			limit: 48,
			search: 'Forest',
		})
		expect(service.getMediaFolder).toHaveBeenCalledExactlyOnceWith(mediaFolder.id)
	})

	it('uses safe defaults for invalid parameters without rewriting them while rendering', async () => {
		await render('/media?folderId=invalid&offset=-1&limit=25')

		expect(service.listMedia).toHaveBeenCalledExactlyOnceWith({
			folderId: undefined,
			offset: 0,
			limit: 24,
			search: undefined,
		})
		expect(router.url).toBe('/media?folderId=invalid&offset=-1&limit=25')
	})

	it('resets offset through URL state when search changes', async () => {
		service.listMedia.mockImplementation((params: ListMediaParams) =>
			of({ ...mediaListing, offset: params.offset ?? 0, total: 100 }),
		)
		const element = await render('/media?offset=24&limit=48')
		const search = getTestElement<HTMLInputElement>('input[placeholder="Search this folder…"]', element)

		search.value = 'Forest'
		search.dispatchEvent(new Event('input', { bubbles: true }))
		await vi.waitFor(() =>
			expect(router.parseUrl(router.url).queryParams).toEqual({ limit: '48', search: 'Forest' }),
		)

		expect(service.listMedia).toHaveBeenLastCalledWith({
			folderId: undefined,
			offset: 0,
			limit: 48,
			search: 'Forest',
		})
	})

	it.each(['escape', 'backdrop'])('routes %s dismissal through the unsaved-change guard', async trigger => {
		await render(`/media/(details:files/${imageAsset.id})`)
		const name = getTestElement<HTMLInputElement>('#media-details-name')

		name.value = 'Unsaved name'
		name.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()

		if (trigger === 'escape') {
			getTestElement('.cdk-overlay-pane').dispatchEvent(
				new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }),
			)
		} else {
			getTestElement<HTMLElement>('.cdk-overlay-backdrop').click()
		}

		await vi.waitFor(() => expect(document.body.textContent).toContain('Discard changes?'))
		getTestButton('Continue editing').click()
		await fixture.whenStable()

		expect(router.url).toContain(imageAsset.id)
		expect(name.value).toBe('Unsaved name')
	})

	it('blocks route departure until discard is confirmed', async () => {
		await render(`/media/(details:files/${imageAsset.id})`)
		const name = getTestElement<HTMLInputElement>('#media-details-name')

		name.value = 'Unsaved name'
		name.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()
		const navigation = router.navigateByUrl('/games')

		await vi.waitFor(() => expect(document.body.textContent).toContain('Discard changes?'))
		getTestButton('Discard changes').click()

		expect(await navigation).toBe(true)
		await fixture.whenStable()

		expect(router.url).toBe('/games')
		expect(document.querySelector('#media-details-name')).toBeNull()
	})

	it('waits for delete confirmation and reconciles successful deletion without refetching the listing', async () => {
		const element = await render()
		const card = fixture.debugElement
			.queryAll(By.directive(MediaCard))
			.find(card => card.componentInstance.item().id === imageAsset.id)

		if (!card) throw new Error('Expected the image card.')

		card.componentInstance.deleted.emit()
		await fixture.whenStable()

		expect(service.deleteMediaAsset).not.toHaveBeenCalled()
		getTestButton('Delete').click()
		await vi.waitFor(() => expect(element.querySelectorAll('media-card')).toHaveLength(2))

		expect(service.deleteMediaAsset).toHaveBeenCalledExactlyOnceWith(imageAsset.id)
		expect(service.listMedia).toHaveBeenCalledTimes(1)
	})

	it('retains a referenced asset and presents the server refusal', async () => {
		service.deleteMediaAsset.mockReturnValue(
			throwError(
				() =>
					new HttpErrorResponse({
						status: HttpStatusCode.Conflict,
						error: { detail: 'This image is in use.' },
					}),
			),
		)
		const notify = vi.spyOn(toast, 'error').mockImplementation(() => '')
		const element = await render()
		const card = fixture.debugElement
			.queryAll(By.directive(MediaCard))
			.find(card => card.componentInstance.item().id === imageAsset.id)

		if (!card) throw new Error('Expected the image card.')

		card.componentInstance.deleted.emit()
		await fixture.whenStable()
		getTestButton('Delete').click()
		await vi.waitFor(() => expect(notify).toHaveBeenCalledWith('This image is in use.'))

		expect(element.querySelectorAll('media-card')).toHaveLength(3)
	})
})
