import { mergeApplicationConfig, ApplicationConfig, Injectable, inject } from '@angular/core'
import { provideServerRendering, withRoutes } from '@angular/ssr'
import { appConfig } from './app.config'
import { serverRoutes } from './app.routes.server'
import {
	FetchBackend,
	HttpBackend,
	HttpEvent,
	HttpRequest,
	HttpResponse,
	provideHttpClient,
} from '@angular/common/http'
import { Observable, of } from 'rxjs'
import games from '../../public/content/games.json'

// Use the published file at build time without a running HTTP server. The normal
// HTTP transfer cache serializes this response for the first browser render.
@Injectable()
class PrerenderCatalogBackend extends HttpBackend {
	readonly #network = inject(FetchBackend)

	override handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
		if (request.method === 'GET' && request.url === '/content/games.json') {
			return of(new HttpResponse({ body: games }))
		}

		return this.#network.handle(request)
	}
}

const serverConfig: ApplicationConfig = {
	providers: [
		provideServerRendering(withRoutes(serverRoutes)),
		provideHttpClient(),
		{ provide: HttpBackend, useClass: PrerenderCatalogBackend },
	],
}

export const config = mergeApplicationConfig(appConfig, serverConfig)
