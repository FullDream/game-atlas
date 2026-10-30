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
				data: { section: 'Games' },
				children: [
					{
						path: '',
						pathMatch: 'full',
						loadChildren: () =>
							import('@game-atlas/games/feature-list').then(module => module.GAMES_ROUTES),
					},
					{
						path: 'new',
						title: 'New game',
						loadChildren: () =>
							import('@game-atlas/games/feature-create').then(module => module.GAME_CREATE_ROUTES),
					},
					{
						path: ':id',
						title: 'Edit game',
						loadChildren: () =>
							import('@game-atlas/games/feature-editor').then(module => module.GAME_EDITOR_ROUTES),
					},
				],
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
