import { TestBed } from '@angular/core/testing'
import { App } from './app'

describe('App', () => {
	it('keeps a neutral public entry without catalog dependencies', async () => {
		await TestBed.configureTestingModule({ imports: [App] }).compileComponents()
		const fixture = TestBed.createComponent(App)

		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('h1')?.textContent).toBe('GameAtlas')
	})
})
