import { provideHttpClient, withXhr } from '@angular/common/http'
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core'
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { getGameQueryKey, gamesListKey, provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { MEDIA_PICKER_SOURCE } from '@game-atlas/shared/util'
import { PageTitleStrategy } from '@game-atlas/shared/ui'
import { provideSignalR } from '@game-atlas/shared/data-access'
import { MediaPickerAdapter, provideMediaBaseUrl } from '@game-atlas/media/data-access'
import { provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { MutationCache, provideTanStackQuery, QueryClient } from '@tanstack/angular-query'
import { withDevtools } from '@tanstack/angular-query-devtools'
import { AdminTitleStrategy } from './admin-title-strategy'
import { APP_ROUTES } from './app.routes'
import { FullscreenOverlayContainer, OverlayContainer } from '@angular/cdk/overlay'

const createQueryClient = (): QueryClient =>
	new QueryClient({
		mutationCache: new MutationCache({
			onSuccess: async (data, variables, onMutateResult, mutation, { client }): Promise<void> => {
				const [domain, gameId, operation] = mutation.options.mutationKey ?? []

				if (domain !== 'maps' || typeof gameId !== 'string') return

				if (!['create', 'update', 'delete', 'add-style', 'delete-style'].includes(String(operation))) return

				await Promise.all([
					client.invalidateQueries({ queryKey: getGameQueryKey(gameId), exact: true }),
					client.invalidateQueries({ queryKey: gamesListKey }),
				])
			},
		}),
	})

export const appConfig: ApplicationConfig = {
	providers: [
		provideBrowserGlobalErrorListeners(),
		{ provide: MEDIA_PICKER_SOURCE, useExisting: MediaPickerAdapter },
		provideRouter(APP_ROUTES, withComponentInputBinding()),
		{ provide: PageTitleStrategy, useClass: AdminTitleStrategy },
		{ provide: TitleStrategy, useExisting: PageTitleStrategy },
		provideHttpClient(withXhr()),
		provideSignalR(),
		provideSpartanHlm(),
		provideBrnDialogDefaultOptions({ backdropClass: 'bg-black/30', ariaLabelledBy: null }),
		provideTanStackQuery(createQueryClient, withDevtools()),
		{ provide: OverlayContainer, useClass: FullscreenOverlayContainer },
		provideGamesBaseUrl('/api'),
		provideMediaBaseUrl('/api'),
	],
}
