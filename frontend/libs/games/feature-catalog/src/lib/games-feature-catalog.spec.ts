import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { ComponentFixture, TestBed } from '@angular/core/testing'
import { GameCatalog } from './games-feature-catalog'

const game = { id: 'a3c5d8e1-7b42-4f90-9a16-2e6d8c4b1f73', slug: 'elden-ring', title: 'Elden Ring', mapsCount: 0 }

describe('GameCatalog page', () => {
	let fixture: ComponentFixture<GameCatalog>
	let httpTestingController: HttpTestingController
	let element: HTMLElement

	beforeEach(() => {
		TestBed.configureTestingModule({
			imports: [GameCatalog],
			providers: [provideHttpClient(), provideHttpClientTesting()],
		})
		httpTestingController = TestBed.inject(HttpTestingController)
		fixture = TestBed.createComponent(GameCatalog)
		element = fixture.nativeElement as HTMLElement
		TestBed.tick()
	})

	afterEach(() => httpTestingController.verify())

	it('shows loading feedback, then renders a non-interactive game card', async () => {
		expect(element.querySelector('[role="status"]')?.textContent).toContain('Loading games')
		httpTestingController.expectOne('/content/games.json').flush([game])
		await fixture.whenStable()
		const card = element.querySelector('article')

		expect(element.querySelector('h1')?.textContent).toBe('Games')
		expect(card?.querySelector('h2')?.textContent?.trim()).toBe(game.title)
		expect(card?.textContent).toContain('0 maps')
		expect(card?.querySelector('a, button')).toBeNull()
		expect(card?.querySelector('img')).toBeNull()
	})

	it('renders optional descriptions and cover images', async () => {
		httpTestingController
			.expectOne('/content/games.json')
			.flush([
				{ ...game, description: 'Explore the Lands Between.', coverUrl: '/assets/games/map-placeholder.svg' },
			])
		await fixture.whenStable()
		expect(element.querySelector('article')?.textContent).toContain('Explore the Lands Between.')
		expect(element.querySelector('img')?.alt).toBe('Elden Ring cover illustration')
	})

	it('shows an empty state', async () => {
		httpTestingController.expectOne('/content/games.json').flush([])
		await fixture.whenStable()
		expect(element.textContent).toContain('No games yet')
		expect(element.querySelector('article')).toBeNull()
	})

	it('offers an accessible retry action after a failure', async () => {
		httpTestingController.expectOne('/content/games.json').flush('', { status: 503, statusText: 'Unavailable' })
		await fixture.whenStable()
		expect(element.querySelector('[role="alert"]')?.textContent).toContain("Couldn't load the games")
		const retry = element.querySelector('button')

		expect(retry?.textContent).toContain('Try again')
		retry?.click()
		TestBed.tick()
		httpTestingController.expectOne('/content/games.json').flush([game])
		await fixture.whenStable()
		expect(element.querySelector('[role="alert"]')).toBeNull()
		expect(element.querySelector('article h2')?.textContent?.trim()).toBe(game.title)
	})
})
