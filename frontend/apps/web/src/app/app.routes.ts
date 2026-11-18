import { Routes } from '@angular/router'
import { PublicGamesQueryOptions } from '@game-atlas/games/data-access'
import { publicMapGameResolver } from './public-map-game-resolver'
import { inject } from '@angular/core'
import { QueryClient } from "@tanstack/angular-query"

export const APP_ROUTES: Routes = [
	{
		path: '',
		loadChildren: () => import('@game-atlas/games/feature-catalog').then(module => module.GAMES_CATALOG_ROUTES),
	},
	{
		path: 'games/:slug/maps/:mapSlug',
		title: async route => {
			const options = inject(PublicGamesQueryOptions)
			const queryClient = inject(QueryClient)

			const gameSlug = route.paramMap.get('slug')

			if (!gameSlug) return ''

			try {
				const data = await queryClient.query(options.getDetail(gameSlug))
				const mapName = data.maps?.find(map => map.slug === route.paramMap.get('mapSlug'))?.name

				return mapName ? `${mapName} - ${data.name}` : data.name
			} catch {
				return ''
			}
		},
		resolve: { game: publicMapGameResolver },
		loadComponent: () => import('@game-atlas/maps/feature-public-map').then(module => module.FeaturePublicMap),
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
