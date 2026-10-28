import { ComponentFixture, TestBed } from '@angular/core/testing'
import { provideRouter } from '@angular/router'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { imageAsset, mediaFolder, videoAsset } from '../media-test-data'
import { MediaCard } from './media-card'

describe('MediaCard', () => {
	let fixture: ComponentFixture<MediaCard>

	beforeEach(() => {
		TestBed.configureTestingModule({ providers: [provideRouter([]), provideSpartanHlm()] })
		fixture = TestBed.createComponent(MediaCard)
	})

	it('uses matching preview and text skeletons without interactive controls', async () => {
		fixture.componentRef.setInput('skeleton', true)
		await fixture.whenStable()
		const element = fixture.nativeElement as HTMLElement

		expect(element.getAttribute('data-skeleton')).toBe('true')
		expect(element.getAttribute('aria-hidden')).toBe('true')
		expect(element.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3)
		expect(element.querySelector('a, button, img')).toBeNull()

		fixture.componentRef.setInput('item', imageAsset)
		fixture.componentRef.setInput('skeleton', false)
		await fixture.whenStable()

		expect(element.querySelector('[data-slot="skeleton"]')).toBeNull()
		expect(element.querySelector('a')?.getAttribute('aria-label')).toBe('View Forest')
		expect(element.hasAttribute('aria-hidden')).toBe(false)
	})

	it('links folders with their server ID and selected page size', async () => {
		fixture.componentRef.setInput('item', mediaFolder)
		fixture.componentRef.setInput('limit', 48)
		await fixture.whenStable()
		const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement

		expect(link.getAttribute('aria-label')).toBe('Open folder Landscapes')
		expect(link.href).toContain(`folderId=${mediaFolder.id}`)
		expect(link.href).toContain('limit=48')
		expect(link.href).not.toContain('files/')
	})

	it('links files to the details outlet and renders their preview', async () => {
		fixture.componentRef.setInput('item', imageAsset)
		await fixture.whenStable()
		const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement

		expect(link.getAttribute('aria-label')).toBe('View Forest')
		expect(link.href).toContain(`details:files/${imageAsset.id}`)
		expect(link.querySelector('img')?.alt).toBe(imageAsset.alt)
	})

	it('uses a video cover when one exists and a video icon otherwise', async () => {
		fixture.componentRef.setInput('item', videoAsset)
		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('img')?.getAttribute('src')).toBe(imageAsset.url)
		fixture.componentRef.setInput('item', { ...videoAsset, preview: null })
		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('img')).toBeNull()
		expect(fixture.nativeElement.querySelector('ng-icon[name="matVideoLibraryRound"]')).not.toBeNull()
	})

	it('shows drop feedback supplied by its parent and disables actions while busy', async () => {
		fixture.componentRef.setInput('item', mediaFolder)
		fixture.componentRef.setInput('dropAllowed', true)
		await fixture.whenStable()

		expect(fixture.nativeElement.textContent).toContain('Drop to move here')
		fixture.componentRef.setInput('dropActive', true)
		fixture.componentRef.setInput('busy', true)
		await fixture.whenStable()

		expect(fixture.nativeElement.textContent).toContain('Release to move here')
		expect(fixture.nativeElement.querySelector('button').disabled).toBe(true)
	})

	it('offers rename, move and delete through its accessible folder menu', async () => {
		const renamed = vi.fn()

		fixture.componentInstance.renamed.subscribe(renamed)
		fixture.componentRef.setInput('item', mediaFolder)
		await fixture.whenStable()
		fixture.nativeElement.querySelector('button[aria-label="Actions for Landscapes"]').click()
		await fixture.whenStable()
		const items = Array.from(document.querySelectorAll<HTMLButtonElement>('[role="menuitem"]'))

		expect(items.map(item => item.textContent?.trim())).toEqual(['Rename', 'Move', 'Delete'])
		items[0].click()

		expect(renamed).toHaveBeenCalledTimes(1)
	})
})
