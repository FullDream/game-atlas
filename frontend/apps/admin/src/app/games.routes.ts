import type { Routes } from '@angular/router'

export const GAMES_ROUTES: Routes = [
	{
		path: '',
		pathMatch: 'full',
		loadChildren: () => import('@game-atlas/games/feature-list').then(module => module.GAMES_ROUTES),
	},
	{
		path: 'new',
		title: 'New game',
		loadChildren: () => import('@game-atlas/games/feature-create').then(module => module.GAME_CREATE_ROUTES),
	},
	{
		path: ':gameId/maps/:mapId/edit',
		title: 'Map editor',
		loadChildren: () => import('./map-editor.routes').then(module => module.MAP_EDITOR_ROUTES),
	},
	{
		path: ':id',
		title: 'Edit game',
		loadChildren: () => import('./game-editor.routes').then(module => module.GAME_EDITOR_ROUTES),
	},
]
