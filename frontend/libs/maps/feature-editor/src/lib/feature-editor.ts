import { HttpStatusCode } from '@angular/common/http'
import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core'
import { toObservable, toSignal } from '@angular/core/rxjs-interop'
import { RouterLink } from '@angular/router'
import { CdkScrollable } from '@angular/cdk/scrolling'
import { MapsQueryOptions, MapsLive, provideMapHub, type MapStyle } from '@game-atlas/maps/data-access'
import { MapViewport } from '@game-atlas/maps/ui'
import { Button, ErrorState, PageHeaderPortal } from '@game-atlas/shared/ui'
import { provideIcons } from '@ng-icons/core'
import {
	matSearchRound,
	matFilterAltRound,
	matRefreshRound,
	matCloudOffRound,
	matArrowBackRound,
	matLayersRound,
} from '@ng-icons/material-symbols/round'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { injectQuery } from '@tanstack/angular-query-experimental'
import { Subject, combineLatest, startWith, switchMap } from 'rxjs'
import { MapStyles } from './map-styles/map-styles'

type SelectionSource = { mapId: string; styles: MapStyle[]; defaultStyleId: string | null }

@Component({
	selector: 'maps-editor',
	imports: [
		Button,
		ErrorState,
		MapViewport,
		PageHeaderPortal,
		RouterLink,
		HlmSkeleton,
		HlmTooltip,
		MapStyles,
		CdkScrollable,
	],
	templateUrl: './feature-editor.html',
	providers: [
		MapsQueryOptions,
		MapsLive,
		provideMapHub(),
		provideIcons({
			matSearchRound,
			matFilterAltRound,
			matRefreshRound,
			matCloudOffRound,
			matArrowBackRound,
			matLayersRound,
		}),
	],
	host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden' },
})
export class FeatureEditor {
	readonly gameId = input.required<string>()
	readonly mapId = input.required<string>()
	readonly gameName = input('Game')

	readonly #options = inject(MapsQueryOptions)
	readonly #live = inject(MapsLive)

	protected readonly map = injectQuery(() => ({
		...this.#options.getDetail(this.gameId(), this.mapId()),
		enabled: false,
	}))

	protected readonly recover = new Subject<void>()
	protected readonly live = toSignal(
		combineLatest([toObservable(this.gameId), toObservable(this.mapId)]).pipe(
			switchMap(([gameId, mapId]) =>
				this.recover.pipe(
					startWith(undefined),
					switchMap(() => this.#live.watch(gameId, mapId)),
				),
			),
		),
		{ initialValue: { status: 'synchronizing' } },
	)

	protected readonly panelOpen = signal(false)
	protected readonly missing = computed(
		() => this.map.error()?.status === HttpStatusCode.NotFound || this.live().status === 'deleted',
	)

	protected readonly breadcrumbs = computed(() => [
		{ label: 'Games', url: '/games' },
		{ label: this.gameName(), url: `/games/${this.gameId()}` },
		{ label: 'Maps', url: `/games/${this.gameId()}/maps` },
		{ label: this.map.data()?.name ?? 'Map editor' },
	])

	protected readonly selectedStyleId = linkedSignal<SelectionSource, string | null>({
		source: () => ({
			mapId: this.mapId(),
			styles: this.map.data()?.styles ?? [],
			defaultStyleId: this.map.data()?.defaultStyleId ?? null,
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
}
