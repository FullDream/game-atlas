import { DOCUMENT } from '@angular/common'
import { BreakpointObserver } from '@angular/cdk/layout'
import { toSignal } from '@angular/core/rxjs-interop'
import { map } from 'rxjs'
import {
	afterNextRender,
	Component,
	computed,
	ElementRef,
	inject,
	Injector,
	input,
	linkedSignal,
	output,
	signal,
} from '@angular/core'
import {
	CategoriesQueryOptions,
	MarkersQueryOptions,
	type Map as GameMap,
	type MapLiveState,
	type MapStyle,
} from '@game-atlas/maps/data-access'
import { Button } from '@game-atlas/shared/ui'
import { provideIcons } from '@ng-icons/core'
import { matChevronLeftRound, matRefreshRound } from '@ng-icons/material-symbols/round'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { injectMutationState, injectQuery } from '@tanstack/angular-query-experimental'
import { MapCategories } from '../map-categories/map-categories'
import { MapMarkers } from '../map-markers/map-markers'
import { MapSettings } from '../map-settings/map-settings'
import { MapStyles } from '../map-styles/map-styles'

import type { MarkerMovementVariables } from '../map-marker-presentation'

type SelectionSource = { mapId: string; styles: MapStyle[]; defaultStyleId: string | null }

@Component({
	selector: 'maps-content-panel',
	imports: [HlmTooltip, Button, HlmAccordionImports, MapCategories, MapMarkers, MapStyles, MapSettings],
	templateUrl: './map-content-panel.html',
	providers: [provideIcons({ matChevronLeftRound, matRefreshRound })],
	host: {
		id: 'map-styles-panel',
		role: 'complementary',
		'aria-label': 'Map content',
		class: 'bg-background md:w-80 min-h-0 flex h-full w-full shrink-0 flex-col overflow-hidden border-r transition-[margin-inline-start,opacity,visibility] duration-200 ease-out motion-reduce:transition-none data-[closed]:-ms-[100%] md:data-[closed]:-ms-80 data-[closed]:opacity-0 data-[closed]:invisible data-[closed]:pointer-events-none',
		'[attr.data-closed]': '!open() ? "" : null',
		'[inert]': '!open()',
		'[attr.aria-hidden]': '!open()',
	},
})
export class MapContentPanel {
	readonly gameId = input.required<string>()
	readonly map = input.required<GameMap>()
	readonly selectedId = input('')
	readonly liveStatus = input.required<MapLiveState['status']>()

	readonly markerSelected = output<string>()
	readonly reconnectRequested = output<void>()

	readonly #breakpoints = inject(BreakpointObserver)
	readonly #document = inject(DOCUMENT)
	readonly #element = inject<ElementRef<HTMLElement>>(ElementRef)
	readonly #injector = inject(Injector)

	readonly #markerOptions = inject(MarkersQueryOptions)
	readonly #categoryOptions = inject(CategoriesQueryOptions)

	protected readonly markers = injectQuery(() => this.#markerOptions.getList(this.gameId(), this.map().id))
	protected readonly categoryGroups = injectQuery(() => this.#categoryOptions.getGroups(this.gameId(), this.map().id))

	protected readonly pendingMovements = injectMutationState(() => ({
		filters: { mutationKey: ['markers', 'update'], status: 'pending' },
		select: mutation => mutation.state.variables as MarkerMovementVariables,
	}))

	readonly narrow = toSignal(this.#breakpoints.observe('(max-width: 767.98px)').pipe(map(state => state.matches)), {
		initialValue: false,
	})
	readonly open = linkedSignal(() => !this.narrow())
	protected readonly activeSection = signal<string | null>('styles')

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
		this.#focusPanelControl()
	}

	close(): void {
		if (!this.narrow()) return

		this.open.set(false)
		this.#focusPanelControl()
	}

	selectStyle(id: string | null): void {
		this.selectedStyleId.set(id)
	}

	protected setSection(section: string, opened: boolean): void {
		if (opened) this.activeSection.set(section)
		else if (this.activeSection() === section) this.activeSection.set(null)
	}

	#focusPanelControl(): void {
		afterNextRender(
			() => {
				if (this.open() && this.narrow()) {
					this.#element.nativeElement
						.querySelector<HTMLButtonElement>(
							'button[data-slot="accordion-trigger"][aria-expanded="true"], button[data-slot="accordion-trigger"]',
						)
						?.focus()
				} else if (!this.open()) {
					this.#document.getElementById('map-content-toggle')?.focus()
				}
			},
			{ injector: this.#injector },
		)
	}
}
