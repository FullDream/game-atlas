import { HttpStatusCode } from '@angular/common/http'
import { Component, computed, DestroyRef, inject, input, linkedSignal, viewChild } from '@angular/core'
import { rxResource } from '@angular/core/rxjs-interop'
import { RouterLink } from '@angular/router'
import {
	CategoriesQueryOptions,
	getMarkerErrorMessage,
	isMarkerOutcomeUncertain,
	MapsLive,
	MapsQueryOptions,
	MarkersQueryOptions,
	provideMapHub,
	type CreateMarkerRequest,
	type CreateMarkerVariables,
	type Marker,
} from '@game-atlas/maps/data-access'
import { MapViewport, type MapPoint, type MapTool, type MarkerMove } from '@game-atlas/maps/ui'
import { Button, ErrorState, PageHeaderPortal } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddLocationAltRound,
	matArrowBackRound,
	matCloudOffRound,
	matChevronLeftRound,
	matChevronRightRound,
	matLibraryAddRound,
	matOpenWithRound,
	matPanToolRound,
	matRefreshRound,
} from '@ng-icons/material-symbols/round'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmSeparator } from '@spartan-ng/helm/separator'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { injectMutation, injectMutationState, injectQuery } from '@tanstack/angular-query-experimental'
import { MapContentPanel } from './map-content-panel/map-content-panel'
import { createMapMarkers, type MarkerMovementVariables, type PlacementState } from './map-marker-presentation'
import { MarkerEditor, type MarkerEditorSession } from './marker-editor/marker-editor'

type MarkerSelectionSource = { mapId: string; markers: Marker[] | undefined }

@Component({
	selector: 'maps-editor',
	imports: [
		MapContentPanel,
		MarkerEditor,
		HlmToggleGroupImports,
		HlmSeparator,
		NgIcon,
		Button,
		ErrorState,
		MapViewport,
		PageHeaderPortal,
		RouterLink,
		HlmSkeleton,
		HlmTooltip,
	],
	templateUrl: './feature-editor.html',
	providers: [
		MarkersQueryOptions,
		CategoriesQueryOptions,
		MapsQueryOptions,
		MapsLive,
		provideMapHub(),
		provideIcons({
			matPanToolRound,
			matOpenWithRound,
			matAddLocationAltRound,
			matLibraryAddRound,

			matChevronLeftRound,
			matChevronRightRound,
			matRefreshRound,
			matCloudOffRound,
			matArrowBackRound,
		}),
	],
	host: {
		class: 'flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden',
		'(document:keydown.escape)': 'finishPlacing()',
	},
})
export class FeatureEditor {
	readonly gameId = input.required<string>()
	readonly mapId = input.required<string>()
	readonly gameName = input('Game')

	readonly #options = inject(MapsQueryOptions)
	readonly #markerOptions = inject(MarkersQueryOptions)
	readonly #categoryOptions = inject(CategoriesQueryOptions)
	readonly #live = inject(MapsLive)
	readonly #destroy = inject(DestroyRef)

	protected readonly contentPanel = viewChild(MapContentPanel)
	protected readonly markerEditor = viewChild(MarkerEditor)
	protected readonly viewport = viewChild(MapViewport)

	protected readonly map = injectQuery(() => ({
		...this.#options.getDetail(this.gameId(), this.mapId()),
		enabled: false,
	}))

	protected readonly markers = injectQuery(() => this.#markerOptions.getList(this.gameId(), this.mapId()))
	protected readonly categoryGroups = injectQuery(() => this.#categoryOptions.getGroups(this.gameId(), this.mapId()))

	protected readonly live = rxResource({
		params: () => ({ gameId: this.gameId(), mapId: this.mapId() }),
		stream: ({ params }) => this.#live.watch(params.gameId, params.mapId),
		defaultValue: { status: 'synchronizing' },
	})

	protected readonly groups = computed(() => this.categoryGroups.data() ?? [])
	protected readonly categories = computed(() => this.groups().flatMap(group => group.categories))
	protected readonly categoriesById = computed(
		() => new Map(this.categories().map(category => [category.id, category])),
	)

	protected readonly missing = computed(
		() => this.map.error()?.status === HttpStatusCode.NotFound || this.live.value().status === 'deleted',
	)

	protected readonly breadcrumbs = computed(() => [
		{ label: 'Games', url: '/games' },
		{ label: this.gameName(), url: `/games/${this.gameId()}` },
		{ label: 'Maps', url: `/games/${this.gameId()}/maps` },
		{ label: this.map.data()?.name ?? 'Map editor' },
	])

	readonly #placementGroup = linkedSignal({ source: this.mapId, computation: () => '' })
	protected readonly placedCount = linkedSignal({ source: this.mapId, computation: () => 0 })
	protected readonly dismissedPlacements = linkedSignal({
		source: this.mapId,
		computation: () => new Set<number>(),
	})

	protected readonly placementStates = injectMutationState<PlacementState>(() => ({
		filters: { mutationKey: ['markers', 'create'] },
		select: mutation => ({
			status: mutation.state.status,
			variables: mutation.state.variables as CreateMarkerVariables | undefined,
			error: mutation.state.error,
			submittedAt: mutation.state.submittedAt,
			data: mutation.state.data as Marker | undefined,
		}),
	}))

	protected readonly movementStates = injectMutationState(() => ({
		filters: { mutationKey: ['markers', 'update'], status: 'pending' },
		select: mutation => mutation.state.variables as MarkerMovementVariables,
	}))

	protected readonly createMarker = injectMutation(() => {
		const options = this.#markerOptions.create()

		return {
			...options,
			onSuccess: async (...args) => {
				await options.onSuccess?.(...args)

				const variables = args[1]

				if (
					!this.#destroy.destroyed &&
					variables.mapId === this.mapId() &&
					variables.requestGroup === this.#placementGroup()
				) {
					this.placedCount.update(count => count + 1)
				}
			},
		}
	})

	protected readonly moveMarker = injectMutation(() => ({
		...this.#markerOptions.update('movement'),
		onError: (error, variables) => {
			if (!this.#destroy.destroyed && variables.mapId === this.mapId()) {
				toast.error(getMarkerErrorMessage(error))
			}
		},
	}))

	protected readonly placements = computed(() =>
		this.placementStates().filter(
			state =>
				state.variables?.mapId === this.mapId() &&
				state.status !== 'success' &&
				!this.dismissedPlacements().has(state.submittedAt),
		),
	)

	protected readonly placementAvailable = computed(
		() =>
			!!this.viewport()?.ready() &&
			!!this.map.data()?.extent &&
			this.categories().length > 0 &&
			!!this.markers.data(),
	)

	protected readonly placementHint = computed(() => {
		if (!this.map.data()?.extent) return 'Attach the first style to initialize map coordinates.'

		if (this.markers.isError()) return 'Retry loading markers before placing new ones.'

		if (this.categoryGroups.isLoading()) return 'Loading categories…'

		if (this.markers.isLoading()) return 'Loading markers…'

		if (!this.viewport()?.ready()) return 'Initializing map…'

		if (this.categoryGroups.isError()) return 'Could not load categories. Retry before placing markers.'

		if (this.categories().length === 0) return 'Add a category before placing markers.'

		return 'Click the map to place a marker.'
	})

	protected readonly tool = linkedSignal<string, MapTool>({
		source: this.mapId,
		computation: () => 'pan',
	})

	protected readonly template = linkedSignal<string, CreateMarkerRequest | null>({
		source: this.mapId,
		computation: () => null,
	})

	protected readonly editorSession = linkedSignal<MarkerSelectionSource, MarkerEditorSession | null>({
		source: () => ({ mapId: this.mapId(), markers: this.markers.data() }),
		computation: (source, previous) => {
			if (source.mapId !== previous?.source.mapId) return null

			const session = previous.value

			if (session?.marker && source.markers && !source.markers.some(marker => marker.id === session.marker?.id)) {
				return null
			}

			return session
		},
	})

	protected readonly selectedId = computed(() => this.editorSession()?.marker?.id ?? '')

	protected readonly viewMarkers = computed(() =>
		createMapMarkers({
			markers: this.markers.data() ?? [],
			groups: this.groups(),
			movements: this.movementStates(),
			mapId: this.mapId(),
			selectedId: this.selectedId(),
			placements: this.placements(),
		}),
	)

	async canLeave(): Promise<boolean> {
		return (await this.markerEditor()?.canLeave()) ?? true
	}

	protected async selectTool(value: unknown): Promise<void> {
		if (value !== 'pan' && value !== 'move' && value !== 'add') return

		if (!(await this.canLeave())) return

		this.editorSession.set(null)
		this.template.set(null)
		this.tool.set(value)
	}

	protected finishPlacing(): void {
		if (this.editorSession()) return

		this.tool.set('pan')
		this.template.set(null)
	}

	protected async selectMarker(id: unknown): Promise<void> {
		if (typeof id !== 'string' || id === this.selectedId()) return

		if (!(await this.canLeave())) return

		const marker = this.markers.data()?.find(marker => marker.id === id)

		if (!marker || this.movementStates().some(state => state.id === marker.id)) return

		this.tool.set('pan')
		this.template.set(null)
		this.contentPanel()?.close()
		this.editorSession.set({ marker, position: marker.position, repeated: false })
	}

	protected savedMarker(marker: Marker): void {
		this.tool.set('pan')
		this.editorSession.set({ marker, position: marker.position, repeated: false })
	}

	protected async repeatMarkers(): Promise<void> {
		if (!this.placementAvailable() || !(await this.canLeave())) return

		this.editorSession.set({ marker: null, position: { x: 0, y: 0 }, repeated: true })
	}

	protected startTemplate(template: CreateMarkerRequest): void {
		this.template.set(template)
		this.#placementGroup.set(crypto.randomUUID())
		this.placedCount.set(0)
		this.tool.set('add')
		this.editorSession.set(null)
	}

	protected placeMarker(position: MapPoint): void {
		if (!this.placementAvailable() || this.editorSession()) return

		const template = this.template()

		if (template) {
			this.createMarker.mutate({
				gameId: this.gameId(),
				mapId: this.mapId(),
				...template,
				position,
				requestGroup: this.#placementGroup(),
			})
		} else {
			this.editorSession.set({ marker: null, position, repeated: false })
		}
	}

	protected move(movement: MarkerMove): void {
		if (this.movementStates().some(state => state.id === movement.id) || this.editorSession()) return

		this.moveMarker.mutate({ gameId: this.gameId(), mapId: this.mapId(), ...movement })
	}

	protected retryPlacement(state: PlacementState): void {
		if (!state.variables || this.placementUncertain(state)) return

		this.dismissPlacement(state.submittedAt)
		this.createMarker.mutate(state.variables)
	}

	protected dismissPlacement(submittedAt: number): void {
		this.dismissedPlacements.update(values => new Set(values).add(submittedAt))
	}

	protected placementUncertain(state: PlacementState): boolean {
		return isMarkerOutcomeUncertain(state.error)
	}

	protected markerCategoryName(id: string): string {
		return this.categoriesById().get(id)?.name ?? 'Category'
	}

	protected revealMarker(bounds: DOMRect): void {
		const viewport = this.viewport()
		const id = this.selectedId()

		if (!viewport || !id || window.innerWidth >= 768) return

		const map = viewport.getBounds()

		viewport.revealMarker(id, {
			left: 0,
			top: 0,
			right: map.width,
			bottom: Math.max(0, Math.min(map.height, bounds.top - map.top)),
		})
	}
}
