import { inject } from '@angular/core'
import { Routes } from '@angular/router'
import { PendingChanges } from '@game-atlas/shared/ui'
import type { MediaDetails } from './media-details/media-details'

export const MEDIA_ROUTES: Routes = [
	{
		path: '',
		providers: [PendingChanges],
		loadComponent: () => import('./feature-list').then(module => module.FeatureList),
		canActivate: [() => inject(PendingChanges).canLeave()],
		canDeactivate: [() => inject(PendingChanges).canLeave()],
		runGuardsAndResolvers: 'always',
		children: [
			{
				path: 'files/:id',
				outlet: 'details',
				loadComponent: () => import('./media-details/media-details').then(module => module.MediaDetails),
				canDeactivate: [(component: MediaDetails) => component.canLeave()],
			},
		],
	},
]
