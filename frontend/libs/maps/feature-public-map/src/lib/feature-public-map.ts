import { Component, inject, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { PublicMapsQueryOptions } from '@game-atlas/maps/data-access'
import { Button } from '@game-atlas/shared/ui'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSpinner } from '@spartan-ng/helm/spinner'
import { injectQuery } from "@tanstack/angular-query"
import type { PublicMapChoice } from './category-filter-panel/category-filter-panel'
import { PublicMapViewer } from './public-map-viewer/public-map-viewer'

export type PublicMapGame = {
	name: string
	coverUrl: string
	bannerUrl: string | null
	maps: readonly PublicMapChoice[]
}

@Component({
	selector: 'maps-feature-public-map',
	imports: [PublicMapViewer, Button, RouterLink, HlmEmptyImports, HlmSpinner],
	templateUrl: './feature-public-map.html',
	providers: [PublicMapsQueryOptions],
	host: { class: 'relative block h-full w-full overflow-hidden' },
})
export class FeaturePublicMap {
	readonly slug = input.required<string>()
	readonly mapSlug = input.required<string>()
	readonly gameTitle = input<string>()
	readonly gameCover = input<string | null>(null)
	readonly game = input<PublicMapGame | null>(null)

	readonly #mapOptions = inject(PublicMapsQueryOptions)

	protected readonly mapDetail = injectQuery(() => this.#mapOptions.getDetail(this.slug(), this.mapSlug()))
}
