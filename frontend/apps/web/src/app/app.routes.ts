import { Routes } from '@angular/router'

export const APP_ROUTES: Routes = [
	{
		path: '',
		loadChildren: () => import('@game-atlas/games/feature-catalog').then(module => module.GAMES_CATALOG_ROUTES),
	},
	{
		path: 'games/:slug',
		title: route => route.paramMap.get('slug') ?? 'Game',
		loadComponent: () => import('./game-placeholder/game-placeholder').then(module => module.GamePlaceholder),
	},
	{
		path: '**',
		redirectTo: '',
	},
]
