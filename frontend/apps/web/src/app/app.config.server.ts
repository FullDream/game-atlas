import { mergeApplicationConfig, ApplicationConfig } from '@angular/core'
import { provideServerRendering, withRoutes } from '@angular/ssr'
import { GAMES_SERVER_URL, provideGamesBaseUrl } from '@game-atlas/games/data-access'
import { appConfig } from './app.config'
import { SERVER_ROUTES } from './app.routes.server'

const serverConfig: ApplicationConfig = {
	providers: [
		provideServerRendering(withRoutes(SERVER_ROUTES)),
		provideGamesBaseUrl(process.env['GAME_ATLAS_API_URL'] ?? GAMES_SERVER_URL),
	],
}

export const config = mergeApplicationConfig(appConfig, serverConfig)
