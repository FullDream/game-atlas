import { Route, Routes } from '@angular/router'

const foundation: NonNullable<Route['loadComponent']> = () =>
	import('./foundation-page').then(page => page.FoundationPage)

export const appRoutes: Routes = [
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
		loadComponent: foundation,
		data: {
			section: 'Media',
			headline: 'Everything in one library.',
			description: 'A shared home for map originals, category icons, images and video.',
			note: 'Media browsing and uploads will be available here.',
			icon: 'matPhotoLibraryRound',
		},
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
]
