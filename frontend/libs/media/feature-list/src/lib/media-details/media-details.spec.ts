import { signal, type WritableSignal } from '@angular/core'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import { provideRouter, ROUTER_OUTLET_DATA } from '@angular/router'
import { MediaService, type MediaAsset, type PaginatedResultOfMediaItem } from '@game-atlas/media/data-access'
import { PendingChanges } from '@game-atlas/shared/ui'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { of, Subject } from 'rxjs'
import { imageAsset, mediaListing, videoAsset } from '../media-test-data'
import { getTestButton, getTestElement } from '../media-test-elements'
import { MediaDetails } from './media-details'

describe('MediaDetails', () => {
	let fixture: ComponentFixture<MediaDetails>
	let outletData: WritableSignal<{ listing: PaginatedResultOfMediaItem; loading: boolean; error: unknown }>
	let updateMediaAsset: ReturnType<typeof vi.fn>

	beforeEach(() => {
		outletData = signal({ listing: mediaListing, loading: false, error: null })
		updateMediaAsset = vi.fn(() => of(imageAsset))

		TestBed.configureTestingModule({
			providers: [
				provideRouter([]),
				provideSpartanHlm(),
				provideTanStackQuery(
					new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } }),
				),
				PendingChanges,
				{ provide: ROUTER_OUTLET_DATA, useValue: outletData },
				{ provide: MediaService, useValue: { updateMediaAsset } },
			],
		})
	})

	afterEach(() => {
		fixture?.destroy()
		TestBed.inject(QueryClient).clear()
	})

	async function render(id = imageAsset.id): Promise<HTMLInputElement> {
		fixture = TestBed.createComponent(MediaDetails)
		fixture.componentRef.setInput('id', id)
		await fixture.whenStable()
		await vi.waitFor(() => expect(document.querySelector('#media-details-name')).not.toBeNull())

		return getTestElement<HTMLInputElement>('#media-details-name')
	}

	it('reads existing listing data and permits leaving a pristine editor', async () => {
		const input = await render()
		const open = vi.spyOn(TestBed.inject(BrnDialogService), 'open')
		const allowed = vi.fn()

		fixture.componentInstance.canLeave().subscribe(allowed)

		expect(input.value).toBe(imageAsset.name)
		expect(document.querySelector<HTMLInputElement>('#media-details-alt')?.value).toBe(imageAsset.alt)
		expect(allowed).toHaveBeenCalledExactlyOnceWith(true)
		expect(open).not.toHaveBeenCalled()
		expect(updateMediaAsset).not.toHaveBeenCalled()
	})

	it('keeps the current draft when the listing refreshes', async () => {
		const input = await render()

		input.value = 'Unsaved draft'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()
		outletData.set({
			listing: { ...mediaListing, items: [{ ...imageAsset, name: 'Refreshed server name' }] },
			loading: false,
			error: null,
		})
		await fixture.whenStable()

		expect(input.value).toBe('Unsaved draft')
	})

	it('submits only dirty fields, keeps an explicitly empty alt and adopts the server baseline', async () => {
		updateMediaAsset.mockReturnValue(of({ ...imageAsset, alt: '' }))
		await render()
		const alt = getTestElement<HTMLInputElement>('#media-details-alt')

		alt.value = ''
		alt.dispatchEvent(new Event('input', { bubbles: true }))
		getTestElement<HTMLFormElement>('form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(updateMediaAsset).toHaveBeenCalledExactlyOnceWith(imageAsset.id, { alt: '' }))
		await fixture.whenStable()
		const allowed = vi.fn()

		fixture.componentInstance.canLeave().subscribe(allowed)

		expect(allowed).toHaveBeenCalledWith(true)
		expect(alt.value).toBe('')
	})

	it('keeps edits when discard is cancelled and shares one confirmation across guard calls', async () => {
		const input = await render()

		input.value = 'Unsaved draft'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()
		const open = vi.spyOn(TestBed.inject(BrnDialogService), 'open')
		const firstDecision = vi.fn()
		const secondDecision = vi.fn()

		fixture.componentInstance.canLeave().subscribe(firstDecision)
		TestBed.inject(PendingChanges).canLeave().subscribe(secondDecision)
		await fixture.whenStable()
		const cancel = getTestButton('Continue editing')

		expect(open).toHaveBeenCalledTimes(1)
		cancel.click()
		await fixture.whenStable()

		expect(firstDecision).toHaveBeenCalledWith(false)
		expect(secondDecision).toHaveBeenCalledWith(false)
		expect(input.value).toBe('Unsaved draft')
	})

	it('discards the draft and resets its dirty state after confirmation', async () => {
		const input = await render()

		input.value = 'Unsaved draft'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		await fixture.whenStable()
		const allowed = vi.fn()

		fixture.componentInstance.canLeave().subscribe(allowed)
		await fixture.whenStable()
		getTestButton('Discard changes').click()
		await fixture.whenStable()

		expect(allowed).toHaveBeenCalledWith(true)
		expect(input.value).toBe(imageAsset.name)
		const nextDecision = vi.fn()

		fixture.componentInstance.canLeave().subscribe(nextDecision)

		expect(nextDecision).toHaveBeenCalledWith(true)
	})

	it('sends null when removing a video cover without resending pristine fields', async () => {
		updateMediaAsset.mockReturnValue(of({ ...videoAsset, preview: null }))
		await render(videoAsset.id)
		const remove = getTestButton('Remove cover')

		remove.click()
		await fixture.whenStable()
		getTestElement<HTMLFormElement>('form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() =>
			expect(updateMediaAsset).toHaveBeenCalledExactlyOnceWith(videoAsset.id, { previewAssetId: null }),
		)
	})

	it('prevents leaving while a save is pending and unlocks after success', async () => {
		const response = new Subject<MediaAsset>()

		updateMediaAsset.mockReturnValue(response)
		const input = await render()

		input.value = 'New name'
		input.dispatchEvent(new Event('input', { bubbles: true }))
		getTestElement<HTMLFormElement>('form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
		await vi.waitFor(() => expect(input.disabled).toBe(true))
		const allowed = vi.fn()

		fixture.componentInstance.canLeave().subscribe(allowed)

		expect(allowed).toHaveBeenCalledWith(false)
		response.next({ ...imageAsset, name: 'New name' })
		response.complete()
		await fixture.whenStable()

		expect(input.disabled).toBe(false)
	})
})
