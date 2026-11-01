import { CdkDrag, CdkDragHandle, CdkDropList, moveItemInArray, type CdkDragDrop } from '@angular/cdk/drag-drop'
import { DOCUMENT } from '@angular/common'
import {
	ChangeDetectorRef,
	Component,
	computed,
	DestroyRef,
	inject,
	input,
	linkedSignal,
	model,
	output,
	signal,
} from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import {
	MapsQueryOptions,
	getMapStyleErrorMessage,
	getMapRetryErrorMessage,
	getMapErrorMessage,
	type MapStyle,
	type Map as GameMap,
} from '@game-atlas/maps/data-access'
import { Button, ConfirmationDialog, MediaThumbnail, TooltipOnOverflow } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matMoreHorizRound,
	matEditRound,
	matDeleteRound,
	matRefreshRound,
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
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { HlmBadge } from '@spartan-ng/helm/badge'
import { HlmProgressImports } from '@spartan-ng/helm/progress'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { StyleEditorDialog } from './style-editor-dialog/style-editor-dialog'

@Component({
	selector: 'maps-styles',
	imports: [
		CdkDrag,
		CdkDragHandle,
		CdkDropList,
		Button,
		MediaThumbnail,
		NgIcon,
		HlmDropdownMenuImports,
		HlmEmptyImports,
		HlmTooltip,
		HlmBadge,
		HlmProgressImports,
		TooltipOnOverflow,
	],
	templateUrl: './map-styles.html',
	styleUrl: './map-styles.css',
	providers: [
		provideIcons({
			matAddRound,
			matMoreHorizRound,
			matEditRound,
			matDeleteRound,
			matRefreshRound,
			matLayersRound,
			matDragIndicatorRound,
			matStarRound,
			matArrowUpwardRound,
			matArrowDownwardRound,
		}),
	],
	host: { class: 'block' },
})
export class MapStyles {
	readonly gameId = input.required<string>()
	readonly map = input.required<GameMap>()
	readonly styleSelected = output<string>()
	readonly selectedStyleId = model<string | null>(null)

	readonly #options = inject(MapsQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #destroy = inject(DestroyRef)
	readonly #document = inject(DOCUMENT)
	readonly #changeDetector = inject(ChangeDetectorRef)

	protected readonly deleting = injectMutation(() => this.#options.deleteStyle(this.gameId(), this.map().id))
	protected readonly presentation = injectMutation(() => this.#options.update(this.gameId(), this.map().id))
	protected readonly retrying = injectMutation(() => this.#options.retryStyle(this.gameId(), this.map().id))

	protected readonly styleError = getMapRetryErrorMessage
	protected readonly mapId = computed(() => this.map().id)
	protected readonly busy = computed(
		() => this.presentation.isPending() || this.deleting.isPending() || this.retrying.isPending(),
	)
	protected readonly dragging = signal(false)
	protected readonly styles = linkedSignal(() => this.map().styles)

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
					this.styles.set(this.map()?.styles ?? [])
					toast.error(getMapErrorMessage(error))
				},
			},
		)
	}

	protected edit(style: MapStyle | null = null): void {
		const map = this.map()

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
		this.styleSelected.emit(style.id)
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
