import { Overlay } from '@angular/cdk/overlay'
import { HlmDrawerImports } from '@spartan-ng/helm/drawer'
import { BrnPopover } from '@spartan-ng/brain/popover'
import { HlmPopoverImports } from '@spartan-ng/helm/popover'
import { Button } from '@game-atlas/shared/ui'
import {
	Component,
	ElementRef,
	afterRenderEffect,
	computed,
	effect,
	inject,
	input,
	linkedSignal,
	signal,
	viewChild,
} from '@angular/core'
import {
	MapClientDb,
	type GroupConfig,
	type PublicMapDetails,
	type PublicCategory,
	type PublicMarker,
} from '@game-atlas/maps/data-access'
import { MapViewport, type MapGeometry, type MapMarker, type MapViewStyle } from '@game-atlas/maps/ui'
import { provideIcons } from '@ng-icons/core'
import { matMenuRound } from '@ng-icons/material-symbols/round'
import { CategoryFilterPanel } from '../category-filter-panel/category-filter-panel'
import { MarkerDrawer } from '../marker-drawer/marker-drawer'
import { MarkerPopup } from '../marker-popup/marker-popup'
import { createPublicMapMarkers } from '../public-marker-presentation'
import { toSignal } from '@angular/core/rxjs-interop'
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout'
import { map } from 'rxjs'
import type { PublicMapGame } from '../feature-public-map'

@Component({
	selector: 'maps-public-map-viewer',
	imports: [
		Button,
		MapViewport,
		CategoryFilterPanel,
		HlmDrawerImports,
		HlmPopoverImports,
		BrnPopover,
		MarkerPopup,
		MarkerDrawer,
	],
	templateUrl: './public-map-viewer.html',
	providers: [provideIcons({ matMenuRound })],
	host: {
		class: 'relative block h-full w-full overflow-hidden',
		'(document:click)': 'onDocumentClick($event)',
	},
})
export class PublicMapViewer {
	readonly slug = input.required<string>()
	readonly data = input.required<PublicMapDetails>()
	readonly gameTitle = input<string>()
	readonly gameCover = input<string | null>(null)
	readonly game = input<PublicMapGame | null>(null)

	readonly #db = inject(MapClientDb)
	readonly #overlay = inject(Overlay)
	readonly #breakpoints = inject(BreakpointObserver)

	protected readonly viewport = viewChild(MapViewport)
	protected readonly popover = viewChild(BrnPopover)
	protected readonly popupAnchor = viewChild<ElementRef<HTMLElement>>('popupAnchor')

	protected readonly popupPositionStrategy = computed(() =>
		this.#overlay
			.position()
			.flexibleConnectedTo(this.popupAnchor()?.nativeElement ?? this.popupPixel())
			.withPositions([
				{ originX: 'center', originY: 'top', overlayX: 'center', overlayY: 'bottom', offsetY: -23 },
				{ originX: 'center', originY: 'bottom', overlayX: 'center', overlayY: 'top', offsetY: 23 },
			])
			.withFlexibleDimensions(false)
			.withPush(true),
	)

	readonly #mapId = computed(() => this.data().id)

	readonly #completedIds = this.#db.liveQuerySignal({
		params: () => this.data().id,
		querier: id =>
			this.#db.completedMarkers
				.where('mapId')
				.equals(id)
				.primaryKeys(ids => new Set(ids)),
		initialValue: new Set<string>(),
	})

	protected readonly visibleCategoryIds = this.#db.liveQuerySignal({
		params: () => this.data().id,
		querier: id =>
			this.#db.visibleCategories
				.where('mapId')
				.equals(id)
				.primaryKeys(ids => new Set(ids)),
		initialValue: new Set<string>(),
	})

	protected readonly groupConfigs = this.#db.liveQuerySignal({
		params: () => this.data().id,
		querier: id =>
			this.#db.groupConfigs
				.where('mapId')
				.equals(id)
				.toArray(rows => new Map(rows.map(row => [row.id, row]))),
		initialValue: new Map<string, GroupConfig>(),
	})

	protected readonly config = this.#db.liveQuerySignal({
		params: () => this.data().id,
		querier: id => this.#db.mapConfig.get(id),
	})

	protected readonly isRightDrawerOpen = linkedSignal({ source: this.#mapId, computation: () => false })
	protected readonly selectedMarkerId = linkedSignal({ source: this.#mapId, computation: (): string | null => null })
	protected readonly popupPixel = signal<{ x: number; y: number }>({ x: 0, y: 0 })

	protected readonly isMobile = toSignal(
		this.#breakpoints.observe(Breakpoints.XSmall).pipe(map(state => state.matches)),
		{ initialValue: false },
	)

	protected readonly isLeftPanelCollapsed = linkedSignal(this.isMobile)
	protected readonly markerDrawerHeight = signal(0)
	protected readonly markerDrawerWidth = signal(0)
	readonly #viewportRevision = signal(0)

	protected readonly gameName = computed(() => {
		return this.game()?.name ?? this.gameTitle() ?? this.slug()
	})

	protected readonly mapGeometry = computed<MapGeometry | null>(() => {
		const data = this.data()

		if (!data.extent) return null

		return {
			id: data.id,
			extent: data.extent,
			tileOrigin: data.tileOrigin,
			tileSize: data.tileSize,
			resolutions: data.resolutions,
			minZoom: data.minZoom,
			maxZoom: data.maxZoom,
		}
	})

	protected readonly mapStyles = computed<MapViewStyle[]>(() => {
		const data = this.data()

		return data.styles.map(style => ({
			id: style.id,
			name: style.name,
			backgroundColor: style.backgroundColor,
			status: 'ready',
			tilesUrlTemplate: style.tilesUrlTemplate,
			tileGeometry: {
				extent: style.tileGeometry.extent,
				tileOrigin: style.tileGeometry.tileOrigin,
				tileSize: style.tileGeometry.tileSize,
				resolutions: style.tileGeometry.resolutions,
				minZoom: data.minZoom ?? 0,
				maxZoom: data.maxZoom ?? 20,
			},
			source: style.preview,
		}))
	})

	protected readonly mapMarkers = computed<MapMarker[]>(() => {
		const data = this.data()

		return createPublicMapMarkers({
			markers: data.markers,
			groups: data.categoryGroups,
			completedIds: this.#completedIds(),
			groupConfigs: this.groupConfigs(),
			visibleCategoryIds: this.visibleCategoryIds(),
			hiddenCategories: this.config()?.hiddenCategories ?? false,
			hideCompleted: this.config()?.hiddenCompleted ?? false,
			selectedId: this.selectedMarkerId(),
		})
	})

	protected readonly selectedMarker = computed<PublicMarker | null>(() => {
		const id = this.selectedMarkerId()

		if (!id) return null

		return this.data().markers.find(m => m.id === id) ?? null
	})

	protected readonly selectedMarkerCategory = computed<PublicCategory | null>(() => {
		const marker = this.selectedMarker()

		if (!marker) return null

		const groups = this.data().categoryGroups ?? []

		for (const group of groups) {
			const found = group.categories.find(c => c.id === marker.categoryId)

			if (found) return found
		}

		return null
	})
	protected readonly isCurrentMarkerCompleted = computed(() => {
		const marker = this.selectedMarker()

		return !!marker && this.#completedIds().has(marker.id)
	})

	constructor() {
		afterRenderEffect(() => {
			this.popupPixel()
			this.popover()?.updatePosition()
		})

		afterRenderEffect(() => {
			const marker = this.selectedMarker()
			const viewport = this.viewport()
			const height = this.markerDrawerHeight()
			const width = this.markerDrawerWidth()
			const mobile = this.isMobile()
			const expanded = this.isRightDrawerOpen()

			this.#viewportRevision()

			if (!marker || !viewport?.ready() || (!mobile && !expanded)) return

			if ((mobile && height <= 0) || (!mobile && width <= 0)) return

			const bounds = viewport.getBounds()

			viewport.revealMarker(
				marker.id,
				{
					left: mobile || this.isLeftPanelCollapsed() ? 8 : 328,
					top: 56,
					right: bounds.width - (mobile ? 8 : width + 8),
					bottom: bounds.height - (mobile ? height + 8 : 8),
				},
				220,
			)
		})

		effect(onCleanup => {
			const viewport = this.viewport()

			if (!viewport?.ready()) return

			const dispose = viewport.onPostRender(() => this.updatePopupPosition())

			onCleanup(() => dispose?.())
		})
	}

	protected onMarkerSelected(id: string): void {
		this.selectedMarkerId.set(id)
		this.updatePopupPosition()
	}

	protected onViewportChanged(): void {
		this.#viewportRevision.update(value => value + 1)
		this.updatePopupPosition()
	}

	protected onDocumentClick(event: MouseEvent): void {
		if (!this.selectedMarkerId() || !(event.target instanceof Element)) return

		if (event.target.closest('maps-viewport, [data-marker-inspector], [data-slot="popover-content"]')) return

		this.closeMarkerInspection()
	}

	protected updatePopupPosition(): void {
		const marker = this.selectedMarker()

		if (!marker) return

		const pixel = this.viewport()?.getPixel(marker.position)

		if (pixel) {
			this.popupPixel.set(pixel)
		}
	}

	protected closeMarkerInspection(): void {
		this.selectedMarkerId.set(null)
		this.isRightDrawerOpen.set(false)
	}

	protected async toggleCurrentMarkerCompleted(): Promise<void> {
		const marker = this.selectedMarker()

		const data = this.data()

		if (!marker) return

		const { id: mapId, gameId } = data
		const { completedMarkers } = this.#db

		await this.#db.transaction('rw', completedMarkers, async () => {
			if (await completedMarkers.get(marker.id)) {
				await completedMarkers.delete(marker.id)
			} else {
				await completedMarkers.put({
					id: marker.id,
					categoryId: marker.categoryId,
					mapId,
					gameId,
					completedAt: new Date().toISOString(),
				})
			}
		})
	}
}
