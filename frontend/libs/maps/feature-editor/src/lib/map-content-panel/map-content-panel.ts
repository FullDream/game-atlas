import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core'
import {
	CategoriesQueryOptions,
	MarkersQueryOptions,
	type Map as GameMap,
	type MapLiveState,
	type MapStyle,
} from '@game-atlas/maps/data-access'
import { Button } from '@game-atlas/shared/ui'
import { provideIcons } from '@ng-icons/core'
import { matRefreshRound } from '@ng-icons/material-symbols/round'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { injectMutationState, injectQuery } from '@tanstack/angular-query-experimental'
import { MapCategories } from '../map-categories/map-categories'
import { MapMarkers } from '../map-markers/map-markers'
import { MapStyles } from '../map-styles/map-styles'

import type { MarkerMovementVariables } from '../map-marker-presentation'

type SelectionSource = { mapId: string; styles: MapStyle[]; defaultStyleId: string | null }

@Component({
	selector: 'maps-content-panel',
	imports: [Button, HlmAccordionImports, MapCategories, MapMarkers, MapStyles],
	templateUrl: './map-content-panel.html',
	providers: [provideIcons({ matRefreshRound })],
	host: {
		id: 'map-styles-panel',
		role: 'complementary',
		'aria-label': 'Map content',
		class: 'bg-background md:flex md:w-80 min-h-0 flex h-full w-full shrink-0 flex-col overflow-hidden border-r',
		'[class.hidden]': '!open()',
	},
})
export class MapContentPanel {
	readonly gameId = input.required<string>()
	readonly map = input.required<GameMap>()
	readonly selectedId = input('')
	readonly liveStatus = input.required<MapLiveState['status']>()

	readonly markerSelected = output<string>()
	readonly reconnectRequested = output<void>()

	readonly #markerOptions = inject(MarkersQueryOptions)
	readonly #categoryOptions = inject(CategoriesQueryOptions)

	protected readonly markers = injectQuery(() => this.#markerOptions.getList(this.gameId(), this.map().id))
	protected readonly categoryGroups = injectQuery(() => this.#categoryOptions.getGroups(this.gameId(), this.map().id))

	protected readonly pendingMovements = injectMutationState(() => ({
		filters: { mutationKey: ['markers', 'update'], status: 'pending' },
		select: mutation => mutation.state.variables as MarkerMovementVariables,
	}))

	readonly open = signal(false)

	readonly selectedStyleId = linkedSignal<SelectionSource, string | null>({
		source: () => ({
			mapId: this.map().id,
			styles: this.map().styles,
			defaultStyleId: this.map().defaultStyleId,
		}),
		computation: (source, previous) => {
			const previousId = previous?.source.mapId === source.mapId ? previous.value : null
			const ready = source.styles.filter(style => style.status === 'ready' && style.tilesUrlTemplate)

			return (
				ready.find(style => style.id === previousId)?.id ??
				ready.find(style => style.id === source.defaultStyleId)?.id ??
				null
			)
		},
	})

	protected readonly categoriesById = computed(
		() =>
			new Map(
				(this.categoryGroups.data() ?? [])
					.flatMap(group => group.categories)
					.map(category => [category.id, category]),
			),
	)

	protected readonly pendingMarkerIds = computed(
		() =>
			new Set(
				this.pendingMovements()
					.filter(state => state.mapId === this.map().id)
					.map(state => state.id),
			),
	)

	toggle(): void {
		this.open.update(open => !open)
	}

	close(): void {
		this.open.set(false)
	}

	selectStyle(id: string | null): void {
		this.selectedStyleId.set(id)
	}
}
