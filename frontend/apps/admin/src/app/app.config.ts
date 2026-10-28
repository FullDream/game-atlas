import { provideHttpClient, withXhr } from '@angular/common/http'
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { provideMediaBaseUrl } from '@game-atlas/media/data-access'
import { provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { withDevtools } from '@tanstack/angular-query-experimental/devtools'
import { AdminTitleStrategy } from './admin-title-strategy'
import { APP_ROUTES } from './app.routes'

export const appConfig: ApplicationConfig = {
	providers: [
		provideBrowserGlobalErrorListeners(),
		provideRouter(APP_ROUTES, withComponentInputBinding()),
		{ provide: TitleStrategy, useClass: AdminTitleStrategy },
		provideHttpClient(withXhr()),
		provideSpartanHlm(),
		provideBrnDialogDefaultOptions({ backdropClass: 'bg-black/30', ariaLabelledBy: null }),
		provideTanStackQuery(new QueryClient(), withDevtools()),
		provideGamesBaseUrl('/api'),
		provideMediaBaseUrl('/api'),
	],
}
