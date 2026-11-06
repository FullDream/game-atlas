import type { FeatureEditor } from '@game-atlas/maps/feature-editor'
import { inject } from '@angular/core'
import type { ActivatedRouteSnapshot, Routes } from '@angular/router'
import { GamesService } from '@game-atlas/games/data-access'
import { MapsService, provideMapHubUrl, provideMapsBaseUrl } from '@game-atlas/maps/data-access'
import { catchError, map, of } from 'rxjs'

export const MAP_EDITOR_ROUTES: Routes = [
	{
		path: '',
		canDeactivate: [(component: FeatureEditor) => component.canLeave()],
		providers: [MapsService, provideMapsBaseUrl('/api'), provideMapHubUrl('/api/hubs/map')],
		resolve: {
			gameId: (route: ActivatedRouteSnapshot) => route.parent?.paramMap.get('gameId'),
			mapId: (route: ActivatedRouteSnapshot) => route.parent?.paramMap.get('mapId'),
			gameName: (route: ActivatedRouteSnapshot) =>
				inject(GamesService)
					.getGame(route.parent?.paramMap.get('gameId') ?? '')
					.pipe(
						map(game => game.name),
						catchError(() => of('Game')),
					),
		},
		loadComponent: () => import('@game-atlas/maps/feature-editor').then(module => module.FeatureEditor),
	},
]
