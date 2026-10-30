import { type Routes } from '@angular/router'
import { type FeatureCreate } from './feature-create'

export const GAME_CREATE_ROUTES: Routes = [
	{
		path: '',
		loadComponent: () => import('./feature-create').then(module => module.FeatureCreate),
		canDeactivate: [(component: FeatureCreate) => component.canLeave()],
	},
]
