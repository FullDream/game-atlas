import { type Routes } from '@angular/router'
import { FeatureEditor } from './feature-editor'

export const GAME_EDITOR_ROUTES: Routes = [
	{
		path: '',
		component: FeatureEditor,
		canDeactivate: [(component: FeatureEditor) => component.canLeave()],
		children: [
			{
				path: '',
				loadComponent: () => import('./game-general/game-general').then(module => module.GameGeneral),
			},
			{
				path: 'maps',
				loadComponent: () => import('./game-maps/game-maps').then(module => module.GameMaps),
			},
			{
				path: 'seo',
				loadComponent: () => import('./game-seo/game-seo').then(module => module.GameSeo),
			},
			{
				path: 'danger-zone',
				loadComponent: () =>
					import('./game-danger-zone/game-danger-zone').then(module => module.GameDangerZone),
			},
		],
	},
]
