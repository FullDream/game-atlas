import { inject } from '@angular/core'
import type { ResolveFn } from '@angular/router'
import { PublicGamesQueryOptions } from '@game-atlas/games/data-access'
import type { PublicMapGame } from '@game-atlas/maps/feature-public-map'
import { QueryClient } from '@tanstack/angular-query'

export const publicMapGameResolver: ResolveFn<PublicMapGame | null> = route => {
	const options = inject(PublicGamesQueryOptions)
	const client = inject(QueryClient)
	const slug = route.paramMap.get('slug') ?? ''

	return client.fetchQuery(options.getDetail(slug)).then(
		game => ({
			name: game.name,
			coverUrl: game.cover.url,
			bannerUrl: game.banner?.url ?? null,
			maps: (game.maps ?? []).map(map => ({ id: map.id, name: map.name, slug: map.slug, preview: map.preview })),
		}),
		() => null,
	)
}
