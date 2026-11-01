import type { Routes } from '@angular/router'
import type { FeatureEditor } from '@game-atlas/games/feature-editor'

export const GAME_EDITOR_ROUTES: Routes = [
	{
		path: '',
		loadComponent: () => import('@game-atlas/games/feature-editor').then(module => module.FeatureEditor),
		canDeactivate: [(component: FeatureEditor) => component.canLeave()],
		children: [
			{
				path: '',
				loadComponent: () => import('@game-atlas/games/feature-editor').then(module => module.GameGeneral),
			},
			{
				path: 'maps',
				loadChildren: () => import('./maps.routes').then(module => module.MAPS_ROUTES),
			},
			{
				path: 'seo',
				loadComponent: () => import('@game-atlas/games/feature-editor').then(module => module.GameSeo),
			},
			{
				path: 'danger-zone',
				loadComponent: () => import('@game-atlas/games/feature-editor').then(module => module.GameDangerZone),
			},
		],
	},
]
