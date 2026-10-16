import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { ApplicationRef } from '@angular/core'
import { TestBed } from '@angular/core/testing'
import { GamesCatalog } from './games-catalog'
import { parseGames } from './game'

const game = { id: 'a3c5d8e1-7b42-4f90-9a16-2e6d8c4b1f73', slug: 'elden-ring', title: 'Elden Ring' }

describe('GamesCatalog', () => {
	let catalog: GamesCatalog
	let http: HttpTestingController

	beforeEach(() => {
		TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] })
		http = TestBed.inject(HttpTestingController)
		catalog = TestBed.inject(GamesCatalog)
		TestBed.tick()
	})

	afterEach(() => http.verify())

	it('loads games from the published JSON and exposes loading state', async () => {
		expect(catalog.games.isLoading()).toBe(true)
		http.expectOne({ method: 'GET', url: '/content/games.json' }).flush([game])
		await TestBed.inject(ApplicationRef).whenStable()
		expect(catalog.games.isLoading()).toBe(false)
		expect(catalog.games.value()).toEqual([game])
		expect(catalog.games.error()).toBeUndefined()
	})

	it('accepts an empty catalog', async () => {
		http.expectOne('/content/games.json').flush([])
		await TestBed.inject(ApplicationRef).whenStable()
		expect(catalog.games.hasValue()).toBe(true)
		expect(catalog.games.value()).toEqual([])
	})

	it('exposes request failures and retries on demand', async () => {
		http.expectOne('/content/games.json').flush('Unavailable', { status: 503, statusText: 'Service Unavailable' })
		await TestBed.inject(ApplicationRef).whenStable()
		expect(catalog.games.error()).toBeDefined()
		expect(catalog.games.hasValue()).toBe(false)
		catalog.games.reload()
		TestBed.tick()
		expect(catalog.games.isLoading()).toBe(true)
		http.expectOne('/content/games.json').flush([game])
		await TestBed.inject(ApplicationRef).whenStable()
		expect(catalog.games.error()).toBeUndefined()
		expect(catalog.games.value()).toEqual([game])
	})

	it('turns invalid content into an error rather than rendering broken cards', async () => {
		http.expectOne('/content/games.json').flush([{ ...game, id: 'elden-ring' }])
		await TestBed.inject(ApplicationRef).whenStable()
		expect(catalog.games.error()).toBeDefined()
	})
})

describe('published catalog validation', () => {
	it.each(
		[
			null,
			{},
			[{ ...game, id: game.id.toUpperCase() }],
			[{ ...game, id: 'a3c5d8e1-7b42-1f90-9a16-2e6d8c4b1f73' }],
			[{ ...game, title: ' ' }],
			[{ ...game, slug: '../elden-ring' }],
			[{ ...game, mapsCount: -1 }],
			[{ ...game, mapsCount: 1.5 }],
			[{ ...game, coverUrl: '//external.example/cover.svg' }],
			[{ ...game, coverUrl: '/assets/../secret.svg' }],
			[game, game],
			[game, { ...game, id: '948c20b4-6cf7-4e57-9735-1c5303491bed' }],
		].map(value => ({ value })),
	)('rejects malformed or duplicate records (%j)', ({ value }) => {
		expect(() => parseGames(value)).toThrow()
	})
})
