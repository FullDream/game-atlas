import { Routes } from '@angular/router'

export const APP_ROUTES: Routes = [
	{
		path: '',
		loadChildren: () => import('@game-atlas/games/feature-catalog').then(module => module.GAMES_CATALOG_ROUTES),
	},
	{
		path: 'games/:slug/maps/:mapSlug',
		title: 'Map',
		children: [],
	},
	{
		path: 'games/:slug',
		title: route => route.paramMap.get('slug') ?? 'Game',
		loadComponent: () => import('@game-atlas/games/feature-detail').then(module => module.GamesFeatureDetail),
	},
	{
		path: '**',
		redirectTo: '',
	},
]
