import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { TestBed } from '@angular/core/testing'
import { QueryClient } from '@tanstack/angular-query-experimental'
import { GamesQueries } from './games-queries'
import { provideGamesBaseUrl } from './generated/api.base-url'

describe('GamesQueries', () => {
	let client: QueryClient

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [provideHttpClient(), provideHttpClientTesting(), provideGamesBaseUrl('/api')],
		})
		client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
	})

	afterEach(() => {
		client.clear()
		TestBed.inject(HttpTestingController).verify()
	})

	it('normalizes defaults and search without introducing sorting', () => {
		const queries = TestBed.inject(GamesQueries)

		expect(queries.list().queryKey).toEqual(queries.list({ offset: 0, limit: 20, search: ' ' }).queryKey)
		expect(queries.list({ search: ' atlas ' }).queryKey).toEqual(queries.list({ search: 'atlas' }).queryKey)
		expect(queries.list().queryKey).not.toEqual(queries.list({ sortBy: 'name' }).queryKey)
		expect(queries.list({ sortBy: 'name' }).queryKey).toEqual(
			queries.list({ sortBy: 'name', sortDirection: 'asc' }).queryKey,
		)
	})

	it('distinguishes pagination, search and sorting in cache keys', () => {
		const queries = TestBed.inject(GamesQueries)
		const base = queries.list().queryKey

		for (const params of [{ offset: 20 }, { limit: 10 }, { search: 'atlas' }, { sortBy: 'updatedAt' as const }]) {
			expect(queries.list(params).queryKey).not.toEqual(base)
		}

		expect(queries.list({ sortBy: 'name', sortDirection: 'desc' }).queryKey).not.toEqual(
			queries.list({ sortBy: 'name', sortDirection: 'asc' }).queryKey,
		)
	})

	it('uses the generated transport and reuses cached data for an identical key', async () => {
		const options = TestBed.inject(GamesQueries).list({ search: 'atlas' })
		const pending = client.fetchQuery(options)
		const request = TestBed.inject(HttpTestingController).expectOne(req => req.url === '/api/games')
		const result = { items: [], limit: 20, offset: 0, total: 0 }

		expect(request.request.params.get('search')).toBe('atlas')
		expect(request.request.params.has('sortBy')).toBe(false)
		request.flush(result)

		expect(await pending).toEqual(result)
		expect(client.getQueryData(options.queryKey)).toEqual(result)
		expect(await client.fetchQuery({ ...options, staleTime: Infinity })).toEqual(result)
	})

	it.each([422, 500])('retains HTTP %s as a rejected query with ProblemDetails', async status => {
		const pending = client.fetchQuery(TestBed.inject(GamesQueries).list())
		const problem = { status, title: 'Request failed', errors: { Search: ['Invalid'] } }
		const rejected = expect(pending).rejects.toMatchObject({ status, error: problem })
		const request = TestBed.inject(HttpTestingController).expectOne(req => req.url === '/api/games')

		request.flush(problem, { status, statusText: 'Request failed' })
		await rejected
	})

	it('cancels the underlying HTTP subscription when the query is cancelled', async () => {
		const options = TestBed.inject(GamesQueries).list()
		const pending = client.fetchQuery(options)
		const rejected = expect(pending).rejects.toThrow()
		const request = TestBed.inject(HttpTestingController).expectOne(req => req.url === '/api/games')

		await client.cancelQueries({ queryKey: options.queryKey })
		await rejected

		expect(request.cancelled).toBe(true)
	})
})
