import { formatDate } from '@angular/common'
import { Component, computed, effect, inject, signal } from '@angular/core'
import { apply, disabled, form, FormRoot } from '@angular/forms/signals'
import { ActivatedRoute, Router, RouterLink } from '@angular/router'
import { GamesQueryOptions, getGameErrorMessage, type Game, type ImageAsset } from '@game-atlas/games/data-access'
import { GameFields, createGameFormModel, gameFormSchema } from '@game-atlas/games/ui'
import { Button, PageHeaderPortal, ConfirmationDialog, MediaPickerDialog } from '@game-atlas/shared/ui'
import { type PickerContext } from '@game-atlas/shared/util'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { provideIcons } from '@ng-icons/core'
import { matAddRound } from '@ng-icons/material-symbols/round'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { defaultIfEmpty, map, Observable, of, take } from 'rxjs'
@Component({
	selector: 'games-create',
	imports: [Button, PageHeaderPortal, RouterLink, FormRoot, GameFields],
	templateUrl: './feature-create.html',
	providers: [provideIcons({ matAddRound })],
	host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col overflow-auto scrollbar-gutter-stable' },
})
export class FeatureCreate {
	readonly #options = inject(GamesQueryOptions)
	readonly #router = inject(Router)
	readonly #route = inject(ActivatedRoute)
	readonly #dialogs = inject(BrnDialogService)
	readonly #create = injectMutation(() => this.#options.create())
	readonly #model = signal(createGameFormModel())

	protected readonly banner = signal<ImageAsset | undefined>(undefined)

	protected readonly cover = signal<ImageAsset | undefined>(undefined)

	protected readonly fields = form(
		this.#model,
		path => {
			apply(path, gameFormSchema)
			disabled(path, { when: () => this.#create.isPending() })
		},
		{
			submission: {
				action: async fields => {
					const model = this.#model()

					await this.#create
						.mutateAsync(
							{
								name: model.name.trim(),
								slug: model.slug.trim(),
								coverId: model.coverId,
								bannerId: model.bannerId || null,
								releaseDate: model.releaseDate
									? formatDate(model.releaseDate, 'yyyy-MM-dd', 'en-US')
									: null,
								developer: model.developer || null,
								description: model.description || null,
							},
							{ onSuccess: () => fields().reset() },
						)
						.catch(() => undefined)
				},
			},
		},
	)

	protected readonly breadcrumbs = [
		{ label: 'Games', url: '/games', queryParams: this.#route.snapshot.queryParams },
		{ label: 'New game' },
	]

	protected readonly error = computed(() => {
		const error = this.#create.error()

		return error ? getGameErrorMessage(error) : ''
	})

	constructor() {
		effect(() => {
			const created = this.#create.data()

			if (created && this.#create.isSuccess() && !this.fields().submitting())
				void this.#router.navigate(['/games', created.id], {
					replaceUrl: true,
					queryParamsHandling: 'preserve',
				})
		})
	}

	canLeave(): Observable<boolean> {
		if (this.fields().submitting()) return of(false)

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

	protected chooseCover(): void {
		this.#dialogs
			.open<PickerContext, Game['cover']>(
				MediaPickerDialog,
				undefined,
				{ mode: 'image' },
				{ ariaLabel: 'Choose a game cover' },
			)
			.closed$.pipe(take(1))
			.subscribe(image => {
				if (!image) return

				this.cover.set(image)
				this.#model.update(model => ({ ...model, coverId: image.id }))
				this.fields.coverId().markAsDirty()
			})
	}

	protected chooseBanner(): void {
		this.#dialogs
			.open<PickerContext, Game['cover']>(
				MediaPickerDialog,
				undefined,
				{ mode: 'image' },
				{ ariaLabel: 'Choose a game banner' },
			)
			.closed$.pipe(take(1))
			.subscribe(image => {
				if (!image) return

				this.banner.set(image)
				this.#model.update(model => ({ ...model, bannerId: image.id }))
				this.fields.bannerId().markAsDirty()
			})
	}
	protected removeBanner(): void {
		this.banner.set(undefined)
		this.#model.update(model => ({ ...model, bannerId: '' }))
		this.fields.bannerId().markAsDirty()
	}
	protected discard(): void {
		this.#model.set(createGameFormModel())
		this.cover.set(undefined)
		this.banner.set(undefined)
		this.fields().reset()
		this.#create.reset()
	}
}
