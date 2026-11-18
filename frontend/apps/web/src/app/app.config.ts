import { provideHttpClient } from '@angular/common/http'
import { ApplicationConfig, inject, PLATFORM_ID, provideBrowserGlobalErrorListeners } from '@angular/core'
import { provideClientHydration, withEventReplay } from '@angular/platform-browser'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { PageTitleStrategy } from '@game-atlas/shared/ui'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query'
import { APP_ROUTES } from './app.routes'
import { WebTitleStrategy } from './web-title-strategy'
import { withDevtools } from '@tanstack/angular-query-devtools'
import { FullscreenOverlayContainer, OverlayContainer } from '@angular/cdk/overlay'
import { isPlatformBrowser } from '@angular/common'

export const createQueryClient = (): QueryClient => {
	const isBrowser = isPlatformBrowser(inject(PLATFORM_ID))

	return new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: isBrowser ? 3 : 0 } } })
}

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
		provideTanStackQuery(createQueryClient, withDevtools()),
		provideGamesBaseUrl('/api'),
	],
}
