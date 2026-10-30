import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { Component, computed, effect, inject, input, linkedSignal } from '@angular/core'
import { apply, disabled, form, FormRoot } from '@angular/forms/signals'
import { ActivatedRoute, Router, RouterLink, RouterOutlet, NavigationEnd } from '@angular/router'
import { GamesQueryOptions, getGameErrorMessage, type Game } from '@game-atlas/games/data-access'
import { GameFields, type GameForm, createGameFormModel, gameFormSchema } from '@game-atlas/games/ui'
import {
	Button,
	ErrorState,
	PageHeaderPortal,
	ConfirmationDialog,
	MediaPickerDialog,
	MediaThumbnail,
} from '@game-atlas/shared/ui'
import { getDirtyValues, type PickerContext } from '@game-atlas/shared/util'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matSaveRound,
	matEditRound,
	matMapRound,
	matSellRound,
	matDeleteRound,
	matCloudOffRound,
	matRefreshRound,
} from '@ng-icons/material-symbols/round'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmAlertImports } from '@spartan-ng/helm/alert'
import { HlmTabsImports } from '@spartan-ng/helm/tabs'
import { toSignal } from '@angular/core/rxjs-interop'
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental'
import { defaultIfEmpty, firstValueFrom, filter, map, Observable, of, take } from 'rxjs'

import { type FeatureEditorContext } from './feature-editor-context'

type FeatureEditorSource = { id: string; game: Game | undefined }

@Component({
	selector: 'games-editor',
	imports: [
		NgIcon,
		HlmSkeleton,
		HlmAlertImports,
		GameFields,
		HlmTabsImports,
		Button,
		ErrorState,
		PageHeaderPortal,
		RouterLink,
		RouterOutlet,
		FormRoot,
		MediaThumbnail,
	],
	templateUrl: './feature-editor.html',
	providers: [
		provideIcons({
			matSaveRound,
			matEditRound,
			matMapRound,
			matSellRound,
			matDeleteRound,
			matCloudOffRound,
			matRefreshRound,
		}),
	],
	host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col overflow-auto scrollbar-gutter-stable' },
})
export class FeatureEditor {
	readonly id = input('', { transform: (value: string | undefined) => value ?? '' })

	readonly #options = inject(GamesQueryOptions)
	readonly #router = inject(Router)
	readonly #route = inject(ActivatedRoute)
	readonly #dialogs = inject(BrnDialogService)

	protected readonly game = injectQuery(() => ({
		...this.#options.getById(this.id()),
		enabled: !!this.id(),
		retry: false,
	}))

	readonly #update = injectMutation(() => this.#options.update(this.id()))
	readonly #delete = injectMutation(() => this.#options.delete(this.id()))

	readonly #model = linkedSignal<FeatureEditorSource, GameForm>({
		source: () => ({ id: this.id(), game: this.game.data() }),
		computation: ({ id, game }, previous) =>
			previous && previous.source.id === id && previous.source.game ? previous.value : createGameFormModel(game),
	})

	readonly #selectedCover = linkedSignal<string, Game['image'] | null>({ source: this.id, computation: () => null })
	protected readonly cover = computed(() => this.#selectedCover() ?? this.game.data()?.image)

	protected readonly fields = form(
		this.#model,
		path => {
			disabled(path, {
				when: () => this.#update.isPending() || this.#delete.isPending(),
			})
			apply(path, gameFormSchema)
		},
		{
			submission: {
				action: async fields => {
					const patch = getDirtyValues(fields)

					if (!Object.keys(patch).length) return

					await this.#update
						.mutateAsync(
							{ id: this.id(), ...patch },
							{
								onSuccess: game => {
									this.#model.set(createGameFormModel(game))
									this.#selectedCover.set(null)
									fields().reset()
								},
							},
						)
						.catch(() => undefined)
				},
			},
		},
	)

	protected readonly breadcrumbs = computed(() => [
		{ label: 'Games', url: '/games', queryParams: this.#route.snapshot.queryParams },
		{ label: this.game.data()?.name ?? 'Edit game' },
	])

	protected readonly loading = computed(() => this.game.isFetching())

	protected readonly error = computed(() => {
		const error = this.#update.error() ?? this.#delete.error()

		return error ? getGameErrorMessage(error) : ''
	})

	protected readonly missing = computed(() => {
		const error = this.game.error()

		return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
	})

	protected readonly deleting = computed(() => this.#delete.isPending())

	protected readonly activeTab = toSignal(
		this.#router.events.pipe(
			filter(event => event instanceof NavigationEnd),
			map(() => this.#route.firstChild?.snapshot?.routeConfig?.path || 'general'),
		),
		{ initialValue: this.#route.firstChild?.snapshot?.routeConfig?.path || 'general' },
	)

	protected readonly outletData = computed<FeatureEditorContext>(() => ({
		fields: this.fields,
		cover: this.cover(),
		editing: true,
		busy: this.fields().submitting() || this.deleting(),
		deleting: this.deleting(),
		gameName: this.game.data()?.name ?? '',
		chooseCover: () => this.chooseCover(),
		changeStatus: value => this.changeStatus(value),
		deleteGame: () => this.deleteGame(),
	}))

	constructor() {
		effect(() => {
			if (this.#delete.isSuccess() && this.#delete.variables() === this.id()) {
				void this.#router.navigate(['/games'], { queryParamsHandling: 'preserve' })
			}
		})
	}

	canLeave(): Observable<boolean> {
		if (this.fields().submitting() || this.#delete.isPending()) return of(false)

		if (!this.fields().dirty()) return of(true)

		return this.#dialogs
			.open(
				ConfirmationDialog,
				undefined,
				{
					title: 'Discard changes?',
					description: 'Your unsaved changes will be lost.',
					confirm: 'Discard changes',
					cancel: 'Continue editing',
					destructive: true,
				},
				{ ariaLabel: 'Discard changes?', role: 'alertdialog' },
			)
			.closed$.pipe(
				take(1),
				map(value => {
					if (value === true) this.discard()

					return value === true
				}),
				defaultIfEmpty(false),
			)
	}

	protected changeStatus(value: unknown): void {
		if (value !== 'draft' && value !== 'published') return

		this.#model.update(model => ({ ...model, status: value }))
		this.fields.status().markAsDirty()
	}

	protected chooseCover(): void {
		const dialog = this.#dialogs.open<PickerContext, Game['image']>(
			MediaPickerDialog,
			undefined,
			{ mode: 'image' },
			{ ariaLabel: 'Choose a game cover' },
		)

		void firstValueFrom(dialog.closed$.pipe(take(1), defaultIfEmpty(undefined))).then(image => {
			if (!image) return

			this.#selectedCover.set(image)
			this.#model.update(model => ({ ...model, imageAssetId: image.id }))
			this.fields.imageAssetId().markAsDirty()
		})
	}

	protected discard(): void {
		this.#model.set(createGameFormModel(this.game.data()))
		this.#selectedCover.set(null)
		this.fields().reset()
		this.#update.reset()
	}

	protected async deleteGame(): Promise<void> {
		const confirmation = this.#dialogs.open(
			ConfirmationDialog,
			undefined,
			{
				title: 'Delete game?',
				description:
					'This permanently deletes the game, all its maps and their content. Library images and other media files are kept.',
				confirm: 'Delete game',
				cancel: 'Cancel',
				destructive: true,
			},
			{ ariaLabel: 'Delete game?', role: 'alertdialog' },
		)
		const confirmed = await firstValueFrom(confirmation.closed$.pipe(take(1), defaultIfEmpty(false)))

		if (confirmed !== true) return

		await this.#delete
			.mutateAsync(this.id(), {
				onSuccess: () => {
					this.fields().reset()
				},
			})

			.catch(() => undefined)
	}
}
