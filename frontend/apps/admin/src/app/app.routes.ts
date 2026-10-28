import { Route, Routes } from '@angular/router'

const foundation: NonNullable<Route['loadComponent']> = () =>
	import('./foundation/foundation-page').then(page => page.FoundationPage)

export const APP_ROUTES: Routes = [
	{
		path: '',
		loadComponent: () => import('./layout/layout').then(module => module.Layout),
		children: [
			{ path: '', pathMatch: 'full', redirectTo: 'games' },
			{
				path: 'games',
				title: 'Games',
				loadComponent: foundation,
				data: {
					section: 'Games',
					headline: 'A home for every game.',
					description: 'Organize the games behind your maps, places and discoveries.',
					note: 'Game management will be available here.',
					icon: 'matSportsEsportsRound',
				},
			},
			{
				path: 'media',
				title: 'Media',
				data: { section: 'Media' },
				loadChildren: () => import('@game-atlas/media/feature-list').then(module => module.MEDIA_ROUTES),
			},
			{
				path: '**',
				title: 'Page not found',
				loadComponent: foundation,
				data: {
					section: 'Page not found',
					headline: 'This page is off the map.',
					description: 'Choose a section from the navigation to get back on track.',
					note: '',
					icon: 'matMapRound',
				},
			},
		],
	},
]
