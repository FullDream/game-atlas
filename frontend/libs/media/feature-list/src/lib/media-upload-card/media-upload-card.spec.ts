import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import type { MediaUploadMutation } from '@game-atlas/media/data-access'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { getTestElement } from '../media-test-elements'
import { MediaUploadCard } from './media-upload-card'

describe('MediaUploadCard', () => {
	let fixture: ComponentFixture<MediaUploadCard>
	let upload: MediaUploadMutation

	beforeEach(() => {
		vi.stubGlobal(
			'URL',
			class extends URL {
				static override createObjectURL = vi.fn(() => 'blob:upload-preview')
				static override revokeObjectURL = vi.fn()
			},
		)
		TestBed.configureTestingModule({ providers: [provideSpartanHlm()] })
		upload = {
			variables: {
				key: 'local-upload-key',
				file: new File(['image'], 'Forest.webp', { type: 'image/webp' }),
				folderId: null,
				controller: new AbortController(),
			},
			status: 'pending',
			submittedAt: 1,
			error: null,
			remove: vi.fn(),
		}
		fixture = TestBed.createComponent(MediaUploadCard)
		fixture.componentRef.setInput('upload', upload)
	})

	afterEach(() => fixture.destroy())

	it('shows transfer progress and creates a local image preview', async () => {
		fixture.componentRef.setInput('percent', 42)
		await fixture.whenStable()
		const element = fixture.nativeElement as HTMLElement

		expect(element.getAttribute('aria-label')).toBe('Upload Forest.webp')
		expect(element.textContent).toContain('42%')
		expect(element.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe('42')
		expect(element.querySelector('img')?.src).toBe('blob:upload-preview')
		expect(URL.createObjectURL).toHaveBeenCalledExactlyOnceWith(upload.variables.file)
	})

	it.each([null, 100])('uses indeterminate progress at %s until the server responds', async percent => {
		fixture.componentRef.setInput('percent', percent)
		await fixture.whenStable()
		const element = fixture.nativeElement as HTMLElement

		expect(element.querySelector('[role="progressbar"]')?.hasAttribute('aria-valuenow')).toBe(false)
		expect(element.textContent).toContain(percent === 100 ? 'Processing' : 'Uploading')
		expect(element.querySelector('hlm-spinner')).not.toBeNull()
	})

	it('emits retry and removal actions for a failed upload', async () => {
		const retry = vi.fn()
		const removed = vi.fn()

		fixture.componentInstance.retry.subscribe(retry)
		fixture.componentInstance.removed.subscribe(removed)
		fixture.componentRef.setInput('upload', {
			...upload,
			status: 'error',
			error: new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { detail: 'Upload refused.' } }),
		})
		await fixture.whenStable()
		const element = fixture.nativeElement as HTMLElement

		expect(element.textContent).toContain('Upload failed')
		getTestElement<HTMLButtonElement>('[aria-label="Retry upload"]', element).click()
		getTestElement<HTMLButtonElement>('[aria-label="Remove upload"]', element).click()

		expect(retry).toHaveBeenCalledTimes(1)
		expect(removed).toHaveBeenCalledTimes(1)
	})

	it('revokes object URLs when the file changes and when the card is destroyed', async () => {
		await fixture.whenStable()
		fixture.componentRef.setInput('upload', {
			...upload,
			variables: { ...upload.variables, file: new File(['other'], 'Other.webp', { type: 'image/webp' }) },
		})
		await fixture.whenStable()

		expect(URL.revokeObjectURL).toHaveBeenCalledTimes(1)
		fixture.destroy()

		expect(URL.revokeObjectURL).toHaveBeenCalledTimes(2)
	})

	it('never embeds a local SVG preview', async () => {
		fixture.componentRef.setInput('upload', {
			...upload,
			variables: { ...upload.variables, file: new File(['<svg/>'], 'Icon.svg', { type: 'image/svg+xml' }) },
		})
		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('img')).toBeNull()
		expect(URL.createObjectURL).not.toHaveBeenCalled()
	})
})
