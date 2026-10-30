import { type Routes } from '@angular/router'

export const GAMES_ROUTES: Routes = [
	{
		path: '',
		title: 'Games',
		loadComponent: () => import('./feature-list').then(module => module.FeatureList),
	},
]
