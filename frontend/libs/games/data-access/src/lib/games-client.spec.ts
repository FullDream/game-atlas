import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { TestBed } from '@angular/core/testing'
import { firstValueFrom } from 'rxjs'
import { provideGamesBaseUrl } from './generated/api.base-url'
import { GamesService } from './generated/games/games.service'
import type { GameDto, UpdateGameRequest } from './generated/models'

const game: GameDto = {
	id: '019a0000-0000-7000-8000-000000000001',
	name: 'GameAtlas',
	slug: 'game-atlas',
	image: 'cover',
	status: 'draft',
	createdAt: '2026-10-25T00:00:00Z',
	updatedAt: '2026-10-25T00:00:00Z',
}

describe('Generated Games client', () => {
	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [provideHttpClient(), provideHttpClientTesting(), provideGamesBaseUrl('/api/')],
		})
	})

	afterEach(() => TestBed.inject(HttpTestingController).verify())

	it('reads the actual paginated contract and camelCase query parameters', async () => {
		const pending = firstValueFrom(
			TestBed.inject(GamesService).listGames({
				offset: 20,
				limit: 10,
				sortBy: 'updatedAt',
				sortDirection: 'desc',
			}),
		)
		const request = TestBed.inject(HttpTestingController).expectOne(req => req.url === '/api/games')

		expect(request.request.params.get('offset')).toBe('20')
		expect(request.request.params.get('sortBy')).toBe('updatedAt')
		expect(request.request.params.get('sortDirection')).toBe('desc')
		request.flush({ items: [game], offset: 20, limit: 10, total: 21 })

		expect(await pending).toEqual({ items: [game], offset: 20, limit: 10, total: 21 })
	})

	it('sends only supplied PATCH fields without requiring response metadata', async () => {
		const patch: UpdateGameRequest = { name: 'Updated' }
		const pending = firstValueFrom(TestBed.inject(GamesService).updateGame(game.id, patch))
		const request = TestBed.inject(HttpTestingController).expectOne(`/api/games/${game.id}`)

		expect(request.request.method).toBe('PATCH')
		expect(request.request.body).toEqual({ name: 'Updated' })
		expect(request.request.body).not.toHaveProperty('status')
		expect(request.request.headers.get('Content-Type') ?? request.request.detectContentTypeHeader()).toMatch(
			/application\/(merge-patch\+)?json/,
		)
		request.flush({ ...game, name: 'Updated' })

		expect((await pending).name).toBe('Updated')
	})

	it('does not silently remove explicit invalid nulls and preserves validation ProblemDetails', async () => {
		// @ts-expect-error Required domain names cannot be null, even though invalid runtime JSON can contain null.
		const invalid: UpdateGameRequest = { name: null }
		const pending = firstValueFrom(TestBed.inject(GamesService).updateGame(game.id, invalid))
		const rejected = expect(pending).rejects.toMatchObject({
			status: 422,
			error: { errors: { Name: ['Required'] } },
		})
		const request = TestBed.inject(HttpTestingController).expectOne(`/api/games/${game.id}`)

		expect(request.request.body).toEqual({ name: null })
		request.flush(
			{ status: 422, errors: { Name: ['Required'] } },
			{ status: 422, statusText: 'Unprocessable Entity' },
		)
		await rejected
	})

	it('creates drafts through the generated operation', async () => {
		const pending = firstValueFrom(
			TestBed.inject(GamesService).createGame({ name: game.name, slug: game.slug, image: game.image }),
		)
		const request = TestBed.inject(HttpTestingController).expectOne('/api/games')

		expect(request.request.method).toBe('POST')
		request.flush(game)

		expect((await pending).id).toBe(game.id)
	})

	it('accepts an empty 204 deletion response', async () => {
		const pending = firstValueFrom(TestBed.inject(GamesService).deleteGame(game.id))
		const request = TestBed.inject(HttpTestingController).expectOne(`/api/games/${game.id}`)

		expect(request.request.method).toBe('DELETE')
		request.flush(null, { status: 204, statusText: 'No Content' })
		await pending
	})

	it('uses another consumer-provided API origin without changing generated code', async () => {
		TestBed.configureTestingModule({ providers: [provideGamesBaseUrl('https://api.example.test/')] })
		const pending = firstValueFrom(TestBed.inject(GamesService).listGames())
		const request = TestBed.inject(HttpTestingController).expectOne('https://api.example.test/games')

		request.flush({ items: [], offset: 0, limit: 20, total: 0 })
		await pending
	})
})
