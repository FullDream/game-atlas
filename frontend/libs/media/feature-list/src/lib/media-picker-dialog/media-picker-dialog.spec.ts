import { DIALOG_DATA } from '@angular/cdk/dialog'
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { signal } from '@angular/core'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import { MediaService, type MediaItemDto } from '@game-atlas/media/data-access'
import { BrnDialogRef } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { of, throwError } from 'rxjs'
import { imageAsset, mediaFolder, mediaListing } from '../media-test-data'
import { getTestButton, getTestElement } from '../media-test-elements'
import { MediaPickerDialog } from './media-picker-dialog'

describe('MediaPickerDialog', () => {
	let fixture: ComponentFixture<MediaPickerDialog>
	let context: { mode: 'image' | 'move'; item?: MediaItemDto; folderId: string | null }
	let close: ReturnType<typeof vi.fn>
	let service: {
		listMedia: ReturnType<typeof vi.fn>
		getMediaFolder: ReturnType<typeof vi.fn>
		updateMediaFolder: ReturnType<typeof vi.fn>
		updateMediaAsset: ReturnType<typeof vi.fn>
	}

	beforeEach(() => {
		context = { mode: 'image', folderId: null }
		close = vi.fn()
		service = {
			listMedia: vi.fn(() => of(mediaListing)),
			getMediaFolder: vi.fn(() => of({ folder: mediaFolder, ancestors: [] })),
			updateMediaFolder: vi.fn(() => of(mediaFolder)),
			updateMediaAsset: vi.fn(() => of(imageAsset)),
		}

		TestBed.configureTestingModule({
			providers: [
				provideSpartanHlm(),
				provideTanStackQuery(
					new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } }),
				),
				{ provide: MediaService, useValue: service },
				{ provide: BrnDialogRef, useValue: { close, state: signal('open') } },
				{ provide: DIALOG_DATA, useValue: context },
			],
		})
	})

	afterEach(() => {
		fixture?.destroy()
		TestBed.inject(QueryClient).clear()
	})

	async function render(): Promise<HTMLElement> {
		fixture = TestBed.createComponent(MediaPickerDialog)
		await fixture.whenStable()
		await vi.waitFor(() =>
			expect(fixture.nativeElement.querySelector('[aria-busy]')?.getAttribute('aria-busy')).toBe('false'),
		)

		return fixture.nativeElement
	}

	it('allows image selection while excluding videos', async () => {
		const element = await render()
		const confirm = getTestButton('Use image', element)

		expect(confirm.disabled).toBe(true)
		expect(element.textContent).not.toContain('Forest walk')
		getTestElement<HTMLButtonElement>('button[aria-pressed]', element).click()
		await fixture.whenStable()

		expect(confirm.disabled).toBe(false)
		confirm.click()

		expect(close).toHaveBeenCalledExactlyOnceWith(imageAsset)
		expect(service.updateMediaAsset).not.toHaveBeenCalled()
	})

	it('passes offset directly and resets it when search changes', async () => {
		service.listMedia.mockReturnValue(of({ ...mediaListing, total: 60 }))
		const element = await render()

		getTestButton('Next', element).click()
		await vi.waitFor(() =>
			expect(service.listMedia).toHaveBeenLastCalledWith({
				folderId: undefined,
				offset: 24,
				limit: 24,
				search: undefined,
			}),
		)
		const search = getTestElement<HTMLInputElement>('input', element)

		search.value = ' Forest '
		search.dispatchEvent(new Event('input', { bubbles: true }))
		await vi.waitFor(() =>
			expect(service.listMedia).toHaveBeenLastCalledWith({
				folderId: undefined,
				offset: 0,
				limit: 24,
				search: 'Forest',
			}),
		)
	})

	it('clears the current selection and search when entering another folder', async () => {
		const element = await render()

		getTestElement<HTMLButtonElement>('button[aria-pressed]', element).click()
		getTestButton(mediaFolder.name, element).click()
		await vi.waitFor(() => expect(service.getMediaFolder).toHaveBeenCalledWith(mediaFolder.id))
		await fixture.whenStable()
		const confirm = getTestButton('Use image', element)

		expect(confirm.disabled).toBe(true)
		expect(service.listMedia).toHaveBeenLastCalledWith({
			folderId: mediaFolder.id,
			offset: 0,
			limit: 24,
			search: undefined,
		})
	})

	it('prevents moving a folder into itself or a descendant', async () => {
		const descendant = { ...mediaFolder, id: '019a1234-5678-7000-8000-000000000004', parentId: mediaFolder.id }

		context.mode = 'move'
		context.item = mediaFolder
		context.folderId = descendant.id
		service.getMediaFolder.mockReturnValue(
			of({ folder: descendant, ancestors: [{ id: mediaFolder.id, name: mediaFolder.name }] }),
		)
		const element = await render()
		const confirm = getTestButton('Move here', element)

		expect(confirm.disabled).toBe(true)
		expect(service.updateMediaFolder).not.toHaveBeenCalled()
		expect(
			Array.from(element.querySelectorAll('button')).find(
				button => button.textContent?.trim() === mediaFolder.name && button.disabled,
			),
		).toBeDefined()
	})

	it('moves an asset to the selected folder and closes only after success', async () => {
		context.mode = 'move'
		context.item = imageAsset
		context.folderId = mediaFolder.id
		const element = await render()

		getTestButton('Move here', element).click()
		await vi.waitFor(() => expect(close).toHaveBeenCalledWith(imageAsset))

		expect(service.updateMediaAsset).toHaveBeenCalledExactlyOnceWith(imageAsset.id, { folderId: mediaFolder.id })
	})

	it('shows transport errors and retries the listing', async () => {
		service.listMedia.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
		const element = await render()

		expect(element.textContent).toContain('Could not reach the server.')
		getTestButton('Retry', element).click()
		await vi.waitFor(() => expect(element.querySelector('button[aria-pressed]')).not.toBeNull())

		expect(service.listMedia).toHaveBeenCalledTimes(2)
	})

	it('retains the picker and presents a refused move', async () => {
		context.mode = 'move'
		context.item = imageAsset
		context.folderId = mediaFolder.id
		service.updateMediaAsset.mockReturnValue(
			throwError(
				() =>
					new HttpErrorResponse({
						status: HttpStatusCode.Conflict,
						error: { detail: 'Destination is unavailable.' },
					}),
			),
		)
		vi.spyOn(toast, 'error').mockImplementation(() => '')
		const element = await render()

		getTestButton('Move here', element).click()
		await vi.waitFor(() => expect(element.textContent).toContain('Destination is unavailable.'))

		expect(close).not.toHaveBeenCalled()
	})
})
