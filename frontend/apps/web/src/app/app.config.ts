import { isPlatformServer } from '@angular/common'
import { provideHttpClient, withFetch } from '@angular/common/http'
import {
	ApplicationConfig,
	InjectionToken,
	makeStateKey,
	PendingTasks,
	PLATFORM_ID,
	provideAppInitializer,
	provideBrowserGlobalErrorListeners,
	TransferState,
	inject,
} from '@angular/core'
import { provideClientHydration, withEventReplay } from '@angular/platform-browser'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { PageTitleStrategy } from '@game-atlas/shared/ui'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import {
	dehydrate,
	type DehydratedState,
	hydrate,
	notifyManager,
	provideTanStackQuery,
	QueryClient,
} from '@tanstack/angular-query-experimental'
import { APP_ROUTES } from './app.routes'
import { WebTitleStrategy } from './web-title-strategy'
import { withDevtools } from '@tanstack/angular-query-experimental/devtools'
import { FullscreenOverlayContainer, OverlayContainer } from '@angular/cdk/overlay'

const WEB_QUERY_CLIENT = new InjectionToken<QueryClient>('WEB_QUERY_CLIENT', {
	factory: () =>
		new QueryClient({
			defaultOptions: {
				queries: {
					staleTime: Infinity,
				},
			},
		}),
})

const WEB_QUERY_STATE_KEY = makeStateKey<DehydratedState>('web-query-state')

export const appConfig: ApplicationConfig = {
	providers: [
		provideClientHydration(withEventReplay()),
		provideBrowserGlobalErrorListeners(),
		provideRouter(APP_ROUTES, withComponentInputBinding()),
		{ provide: PageTitleStrategy, useClass: WebTitleStrategy },
		{ provide: TitleStrategy, useExisting: PageTitleStrategy },
		{ provide: OverlayContainer, useClass: FullscreenOverlayContainer },
		provideHttpClient(),
		provideSpartanHlm(),
		provideTanStackQuery(WEB_QUERY_CLIENT, withDevtools()),
		provideAppInitializer(() => {
			const transferState = inject(TransferState)
			const queryClient = inject(QueryClient)
			const platformId = inject(PLATFORM_ID)

			if (isPlatformServer(platformId)) {
				const pendingTasks = inject(PendingTasks)
				const activeTasks = new Map<string, () => void>()

				queryClient.getQueryCache().subscribe(event => {
					const queryHash = event.query.queryHash

					if (event.query.state.fetchStatus === 'fetching') {
						if (!activeTasks.has(queryHash)) {
							activeTasks.set(queryHash, pendingTasks.add())
						}
					} else {
						const remove = activeTasks.get(queryHash)

						if (remove) {
							activeTasks.delete(queryHash)
							notifyManager.schedule(() => queueMicrotask(remove))
						}
					}
				})

				transferState.onSerialize(WEB_QUERY_STATE_KEY, () => dehydrate(queryClient))

				return
			}

			const state = transferState.get(WEB_QUERY_STATE_KEY, null)

			if (state) {
				hydrate(queryClient, state)
				transferState.remove(WEB_QUERY_STATE_KEY)
			}
		}),
		provideGamesBaseUrl('/api'),
	],
}
