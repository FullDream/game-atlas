import { TestBed } from '@angular/core/testing'
import { App } from './app'
import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { Router, provideRouter } from '@angular/router'
import { appRoutes } from './app.routes'

describe('App', () => {
	beforeEach(async () => {
		await TestBed.configureTestingModule({
			imports: [App],
			providers: [provideRouter(appRoutes), provideHttpClient(), provideHttpClientTesting()],
		}).compileComponents()
	})

	it('opens the lazy catalog at the home route', async () => {
		const fixture = TestBed.createComponent(App)
		await TestBed.inject(Router).navigateByUrl('/')
		TestBed.tick()
		TestBed.inject(HttpTestingController).expectOne('/content/games.json').flush([])
		await fixture.whenStable()
		const compiled = fixture.nativeElement as HTMLElement
		expect(compiled.querySelector('h1')?.textContent).toBe('Games')
		expect(compiled.textContent).toContain('No games yet')
		TestBed.inject(HttpTestingController).verify()
	})
})
