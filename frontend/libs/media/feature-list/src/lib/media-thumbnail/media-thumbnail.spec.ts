import { TestBed } from '@angular/core/testing'
import { imageAsset } from '../media-test-data'
import { MediaThumbnail } from './media-thumbnail'

describe('MediaThumbnail', () => {
	it('renders the server image URL and alternative text', async () => {
		const fixture = TestBed.createComponent(MediaThumbnail)

		fixture.componentRef.setInput('image', imageAsset)
		await fixture.whenStable()
		const image = fixture.nativeElement.querySelector('img') as HTMLImageElement

		expect(image.getAttribute('src')).toBe(imageAsset.url)
		expect(image.alt).toBe(imageAsset.alt)
	})

	it('shows a fallback after delivery fails and recovers when a new image is supplied', async () => {
		const fixture = TestBed.createComponent(MediaThumbnail)

		fixture.componentRef.setInput('image', imageAsset)
		await fixture.whenStable()
		fixture.nativeElement.querySelector('img').dispatchEvent(new Event('error'))
		await fixture.whenStable()

		expect(fixture.nativeElement.textContent).toContain('Image unavailable')
		expect(fixture.nativeElement.querySelector('img')).toBeNull()

		fixture.componentRef.setInput('image', { ...imageAsset, url: '/media/files/replacement.webp' })
		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('img')?.getAttribute('src')).toBe('/media/files/replacement.webp')
		expect(fixture.nativeElement.textContent).not.toContain('Image unavailable')
	})
})
