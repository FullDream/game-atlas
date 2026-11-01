import { CdkDrag, CdkDragHandle, CdkDropList, moveItemInArray, type CdkDragDrop } from '@angular/cdk/drag-drop'
import { DOCUMENT } from '@angular/common'
import { HttpStatusCode } from '@angular/common/http'
import { ChangeDetectorRef, Component, computed, DestroyRef, inject, input, linkedSignal, signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { RouterLink } from '@angular/router'
import {
	MapsQueryOptions,
	getMapStyleErrorMessage,
	type MapStyle,
	getMapErrorMessage,
} from '@game-atlas/maps/data-access'
import { MapViewport } from '@game-atlas/maps/ui'
import {
	Button,
	ConfirmationDialog,
	ErrorState,
	MediaThumbnail,
	PageHeaderPortal,
	TooltipOnOverflow,
} from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matMoreHorizRound,
	matEditRound,
	matDeleteRound,
	matSearchRound,
	matFilterAltRound,
	matMapRound,
	matRefreshRound,
	matCloudOffRound,
	matArrowBackRound,
	matLayersRound,
	matDragIndicatorRound,
	matStarRound,
	matArrowUpwardRound,
	matArrowDownwardRound,
} from '@ng-icons/material-symbols/round'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { HlmBadge } from '@spartan-ng/helm/badge'
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { StyleEditorDialog } from './style-editor-dialog/style-editor-dialog'

type SelectionSource = { mapId: string; styles: MapStyle[]; defaultStyleId: string | null }

@Component({
	selector: 'maps-editor',
	imports: [
		CdkDrag,
		CdkDragHandle,
		CdkDropList,
		Button,
		ErrorState,
		MapViewport,
		MediaThumbnail,
		PageHeaderPortal,
		RouterLink,
		NgIcon,
		HlmDropdownMenuImports,
		HlmEmptyImports,
		HlmSkeleton,
		HlmTooltip,
		HlmBadge,
		TooltipOnOverflow,
	],
	templateUrl: './feature-editor.html',
	styleUrl: './feature-editor.css',
	providers: [
		provideIcons({
			matAddRound,
			matMoreHorizRound,
			matEditRound,
			matDeleteRound,
			matSearchRound,
			matFilterAltRound,
			matMapRound,
			matRefreshRound,
			matCloudOffRound,
			matArrowBackRound,
			matLayersRound,
			matDragIndicatorRound,
			matStarRound,
			matArrowUpwardRound,
			matArrowDownwardRound,
		}),
	],
	host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden' },
})
export class FeatureEditor {
	readonly gameId = input.required<string>()
	readonly mapId = input.required<string>()
	readonly gameName = input('Game')

	readonly #options = inject(MapsQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #destroy = inject(DestroyRef)
	readonly #document = inject(DOCUMENT)
	readonly #changeDetector = inject(ChangeDetectorRef)

	protected readonly map = injectQuery(() => ({
		...this.#options.getDetail(this.gameId(), this.mapId()),
		enabled: !!this.gameId() && !!this.mapId(),
		refetchOnMount: 'always' as const,
	}))

	protected readonly deleting = injectMutation(() => this.#options.deleteStyle(this.gameId(), this.mapId()))

	protected readonly presentation = injectMutation(() => this.#options.update(this.gameId(), this.mapId()))

	protected readonly busy = computed(() => this.presentation.isPending() || this.deleting.isPending())

	protected readonly panelOpen = signal(false)
	protected readonly dragging = signal(false)
	protected readonly styles = linkedSignal(() => this.map.data()?.styles ?? [])
	protected readonly missing = computed(() => this.map.error()?.status === HttpStatusCode.NotFound)

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

	protected makeDefault(style: MapStyle): void {
		this.presentation.mutate(
			{ id: this.mapId(), patch: { defaultStyleId: style.id } },
			{
				onError: error => toast.error(getMapErrorMessage(error)),
			},
		)
	}

	protected drop(event: CdkDragDrop<MapStyle[]>): void {
		this.move(event.previousIndex, event.currentIndex)
	}

	protected move(from: number, to: number): void {
		const styles = [...this.styles()]

		if (from === to || to < 0 || to >= styles.length || this.busy()) return

		moveItemInArray(styles, from, to)
		this.styles.set(styles)
		this.#changeDetector.detectChanges()

		this.presentation.mutate(
			{ id: this.mapId(), patch: { styleOrder: styles.map(style => style.id) } },
			{
				onError: error => {
					this.styles.set(this.map.data()?.styles ?? [])
					toast.error(getMapErrorMessage(error))
				},
			},
		)
	}

	protected edit(style: MapStyle | null = null): void {
		const map = this.map.data()

		if (!map) return

		this.#dialogs.open(
			StyleEditorDialog,
			undefined,
			{ gameId: this.gameId(), map, style },
			{
				providers: [{ provide: MapsQueryOptions, useValue: this.#options }],
				ariaLabel: style ? 'Edit style' : 'Add style',
				restoreFocus: style ? '#style-actions-' + style.id : '#style-add',
				closeOnOutsidePointerEvents: false,
				disableClose: true,
			},
		)
	}

	protected select(style: MapStyle): void {
		if (style.status !== 'ready' || !style.tilesUrlTemplate) return

		this.selectedStyleId.set(style.id)
		this.panelOpen.set(false)
	}

	protected remove(style: MapStyle): void {
		this.#dialogs
			.open(
				ConfirmationDialog,
				undefined,
				{
					title: `Delete “${style.name}”?`,
					description:
						'This removes the style and its generated tiles. Library media, map coordinates, categories and markers are kept.',
					confirm: 'Delete style',
					cancel: 'Cancel',
					destructive: true,
				},
				{ ariaLabel: 'Delete style?', role: 'alertdialog', restoreFocus: '#style-actions-' + style.id },
			)
			.closed$.pipe(take(1), takeUntilDestroyed(this.#destroy))
			.subscribe(confirmed => {
				if (confirmed !== true) return

				this.deleting.mutate(style.id, {
					onError: error => toast.error(getMapStyleErrorMessage(error)),
					onSuccess: () => this.#document.getElementById('style-add')?.focus(),
				})
			})
	}
}
