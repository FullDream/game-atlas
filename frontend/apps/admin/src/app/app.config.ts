import { provideHttpClient } from '@angular/common/http'
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { appRoutes } from './app.routes'
import { AdminTitleStrategy } from './admin-title-strategy'

export const appConfig: ApplicationConfig = {
	providers: [
		provideBrowserGlobalErrorListeners(),
		provideRouter(appRoutes, withComponentInputBinding()),
		{ provide: TitleStrategy, useClass: AdminTitleStrategy },
		provideHttpClient(),
		provideSpartanHlm(),
		provideTanStackQuery(new QueryClient()),
		provideGamesBaseUrl('/api'),
	],
}
