import { Route } from '@angular/router'

export const appRoutes: Route[] = [
	{
		path: '',
		pathMatch: 'full',
		title: 'Games | Game Atlas',
		loadComponent: () => import('@game-atlas/games/feature-catalog').then(m => m.GameCatalog),
	},
]
