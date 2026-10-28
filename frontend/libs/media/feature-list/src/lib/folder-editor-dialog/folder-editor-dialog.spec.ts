import { DIALOG_DATA } from '@angular/cdk/dialog'
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { signal } from '@angular/core'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import { MediaService, type MediaFolderDto } from '@game-atlas/media/data-access'
import { BrnDialogRef } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { of, Subject, throwError } from 'rxjs'
import { mediaFolder } from '../media-test-data'
import { FolderEditorDialog } from './folder-editor-dialog'

describe('FolderEditorDialog', () => {
	let fixture: ComponentFixture<FolderEditorDialog>
	let context: { folder: MediaFolderDto | null; parentId: string | null }
	let service: { createMediaFolder: ReturnType<typeof vi.fn>; updateMediaFolder: ReturnType<typeof vi.fn> }
	let close: ReturnType<typeof vi.fn>

	beforeEach(() => {
		context = { folder: null, parentId: mediaFolder.id }
		close = vi.fn()
		service = {
			createMediaFolder: vi.fn(() => of(mediaFolder)),
			updateMediaFolder: vi.fn(() => of({ ...mediaFolder, name: 'Renamed' })),
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

	async function render(): Promise<{ input: HTMLInputElement; form: HTMLFormElement; save: HTMLButtonElement }> {
		fixture = TestBed.createComponent(FolderEditorDialog)
		await fixture.whenStable()

		return {
			input: fixture.nativeElement.querySelector('input'),
			form: fixture.nativeElement.querySelector('form'),
			save: fixture.nativeElement.querySelector('button:not([type="button"])'),
		}
	}

	it('validates an empty name on submit and sends no request', async () => {
		const { form } = await render()

		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await fixture.whenStable()

		expect(fixture.nativeElement.textContent).toContain('Name is required.')
		expect(service.createMediaFolder).not.toHaveBeenCalled()
		expect(close).not.toHaveBeenCalled()
	})

	it('rejects an overlong name through the form validator', async () => {
		const { input, form } = await render()

		input.value = 'a'.repeat(201)
		input.dispatchEvent(new Event('input', { bubbles: true }))
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await fixture.whenStable()

		expect(fixture.nativeElement.textContent).toContain('Name must be at most 200 characters.')
		expect(service.createMediaFolder).not.toHaveBeenCalled()
	})

	it('creates in the captured parent and closes after success', async () => {
		const { input, form } = await render()

		input.value = 'New folder'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(close).toHaveBeenCalledExactlyOnceWith(true))

		expect(service.createMediaFolder).toHaveBeenCalledExactlyOnceWith({
			name: 'New folder',
			parentId: mediaFolder.id,
		})
		expect(service.updateMediaFolder).not.toHaveBeenCalled()
	})

	it('disables an unchanged rename and sends only the dirty name when edited', async () => {
		context.folder = mediaFolder
		const { input, form, save } = await render()

		expect(input.value).toBe(mediaFolder.name)
		expect(save.disabled).toBe(true)
		input.value = 'Renamed'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()

		expect(save.disabled).toBe(false)
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(close).toHaveBeenCalledWith(true))

		expect(service.updateMediaFolder).toHaveBeenCalledExactlyOnceWith(mediaFolder.id, { name: 'Renamed' })
		expect(service.createMediaFolder).not.toHaveBeenCalled()
	})

	it('locks controls during submission and prevents duplicate requests', async () => {
		const response = new Subject<MediaFolderDto>()

		service.createMediaFolder.mockReturnValue(response)
		const { input, form, save } = await render()

		input.value = 'New folder'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(input.disabled).toBe(true))

		expect(save.disabled).toBe(true)
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		expect(service.createMediaFolder).toHaveBeenCalledTimes(1)
		expect(close).not.toHaveBeenCalled()
		response.next(mediaFolder)
		response.complete()
		await fixture.whenStable()
	})

	it('keeps valid input after a server refusal and allows retry without another edit', async () => {
		const error = new HttpErrorResponse({
			status: HttpStatusCode.Conflict,
			error: { detail: 'Name already exists.' },
		})
		const notify = vi.spyOn(toast, 'error').mockImplementation(() => '')

		service.createMediaFolder.mockReturnValueOnce(throwError(() => error))
		const { input, form } = await render()

		input.value = 'New folder'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(fixture.nativeElement.textContent).toContain('Name already exists.'))

		expect(close).not.toHaveBeenCalled()
		expect(input.value).toBe('New folder')
		expect(notify).toHaveBeenCalledWith('Name already exists.')
		form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(close).toHaveBeenCalledWith(true))

		expect(service.createMediaFolder).toHaveBeenCalledTimes(2)
	})
})
