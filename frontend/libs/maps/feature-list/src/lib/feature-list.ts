import { Component, computed, inject, input, signal } from '@angular/core'
import { DatePipe, DOCUMENT } from '@angular/common'
import { RouterLink } from '@angular/router'
import { MapsQueryOptions, getMapErrorMessage, type Map as GameMap } from '@game-atlas/maps/data-access'
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
	matMapRound,
	matMoreHorizRound,
	matEditRound,
	matDeleteRound,
	matRefreshRound,
	matCloudOffRound,
} from '@ng-icons/material-symbols/round'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { MapEditorDialog } from './map-editor-dialog/map-editor-dialog'

@Component({
	selector: 'maps-list',
	imports: [
		RouterLink,
		Button,
		ErrorState,
		MediaThumbnail,
		PageHeaderPortal,
		NgIcon,
		DatePipe,
		HlmDropdownMenuImports,
		HlmEmptyImports,
		HlmSkeleton,
		HlmTooltip,
		TooltipOnOverflow,
	],
	templateUrl: './feature-list.html',
	styleUrl: './feature-list.css',
	providers: [
		provideIcons({
			matAddRound,
			matMapRound,
			matMoreHorizRound,
			matEditRound,
			matDeleteRound,
			matRefreshRound,
			matCloudOffRound,
		}),
	],
	host: { class: 'block min-w-0 rounded-xl border bg-card p-4 text-card-foreground md:p-6' },
})
export class FeatureList {
	readonly id = input.required<string>()

	readonly #options = inject(MapsQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #document = inject(DOCUMENT)

	protected readonly maps = injectQuery(() => ({
		...this.#options.getList(this.id()),
		enabled: !!this.id(),
		staleTime: Infinity,
		refetchOnMount: false,
		refetchOnWindowFocus: false,
		refetchOnReconnect: false,
		retry: false,
	}))

	readonly #deletingId = signal('')

	protected readonly deleting = injectMutation(() => this.#options.delete(this.id(), this.#deletingId()))

	protected readonly count = computed(() => this.maps.data()?.length ?? 0)

	protected preview(map: GameMap): GameMap['styles'][number]['source'] | null {
		return map.styles.find(style => style.id === map.defaultStyleId)?.source ?? null
	}

	protected edit(map: GameMap | null = null): void {
		this.#dialogs.open(
			MapEditorDialog,
			undefined,
			{ gameId: this.id(), map },
			{
				providers: [{ provide: MapsQueryOptions, useValue: this.#options }],
				ariaLabel: map ? 'Edit map' : 'Add map',
				restoreFocus: map ? '#map-actions-' + map.id : '#maps-add',
				closeOnOutsidePointerEvents: false,
				disableClose: true,
			},
		)
	}

	protected remove(map: GameMap): void {
		this.#deletingId.set(map.id)

		this.#dialogs
			.open(
				ConfirmationDialog,
				undefined,
				{
					title: `Delete “${map.name}”?`,
					description:
						'This permanently deletes the map, its styles, category groups, categories and markers. Library media files are kept.',
					confirm: 'Delete map',
					cancel: 'Cancel',
					destructive: true,
				},
				{ ariaLabel: 'Delete map?', role: 'alertdialog', restoreFocus: '#map-actions-' + map.id },
			)
			.closed$.pipe(take(1))
			.subscribe(confirmed => {
				if (confirmed !== true) return

				this.deleting.mutate(map.id, {
					onSuccess: () => this.#document.getElementById('maps-add')?.focus(),
					onError: error => toast.error(getMapErrorMessage(error)),
				})
			})
	}
}
