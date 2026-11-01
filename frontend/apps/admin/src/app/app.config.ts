import { provideHttpClient, withXhr } from '@angular/common/http'
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { getGameQueryKey, provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { MEDIA_PICKER_SOURCE } from '@game-atlas/shared/util'
import { MediaPickerAdapter, provideMediaBaseUrl } from '@game-atlas/media/data-access'
import { provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { MutationCache, provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental'
import { withDevtools } from '@tanstack/angular-query-experimental/devtools'
import { AdminTitleStrategy } from './admin-title-strategy'
import { APP_ROUTES } from './app.routes'

const queryClient = new QueryClient({
	mutationCache: new MutationCache({
		onSuccess: async (data, variables, context, mutation): Promise<void> => {
			const [domain, gameId, operation] = mutation.options.mutationKey ?? []

			if (domain !== 'maps' || typeof gameId !== 'string') return

			if (operation !== 'create' && operation !== 'update' && operation !== 'delete') return

			await queryClient.invalidateQueries({ queryKey: getGameQueryKey(gameId), exact: true })
		},
	}),
})

export const appConfig: ApplicationConfig = {
	providers: [
		provideBrowserGlobalErrorListeners(),
		{ provide: MEDIA_PICKER_SOURCE, useExisting: MediaPickerAdapter },
		provideRouter(APP_ROUTES, withComponentInputBinding()),
		{ provide: TitleStrategy, useClass: AdminTitleStrategy },
		provideHttpClient(withXhr()),
		provideSpartanHlm(),
		provideBrnDialogDefaultOptions({ backdropClass: 'bg-black/30', ariaLabelledBy: null }),
		provideTanStackQuery(queryClient, withDevtools()),
		provideGamesBaseUrl('/api'),
		provideMediaBaseUrl('/api'),
	],
}
