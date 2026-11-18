import { HttpErrorResponse } from '@angular/common/http'
import { inject, Service } from '@angular/core'
import { type CreateQueryOptions, queryOptions } from "@tanstack/angular-query"
import { toPromise } from '@game-atlas/shared/util'
import { PublicMapsService } from './generated/public-maps/public-maps.service'
import type { PublicMapDetails } from './generated/models'

export const publicMapQueryKeys = {
	detail: (gameSlug: string, mapSlug: string) => ['public-maps', 'detail', gameSlug, mapSlug] as const,
}

@Service()
export class PublicMapsQueryOptions {
	readonly #api = inject(PublicMapsService)

	getDetail(gameSlug: string, mapSlug: string): CreateQueryOptions<PublicMapDetails, HttpErrorResponse> {
		return queryOptions<PublicMapDetails, HttpErrorResponse>({
			queryKey: publicMapQueryKeys.detail(gameSlug, mapSlug),
			queryFn: ({ signal }) => toPromise(this.#api.getPublicMap(gameSlug, mapSlug), signal),
			staleTime: 60_000,
			enabled: Boolean(gameSlug && mapSlug),
		})
	}
}
