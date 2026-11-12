import { NgTemplateOutlet } from '@angular/common'
import { BreakpointObserver } from '@angular/cdk/layout'
import {
	afterRenderEffect,
	Component,
	computed,
	DestroyRef,
	effect,
	ElementRef,
	inject,
	input,
	linkedSignal,
	output,
	signal,
	untracked,
	viewChild,
} from '@angular/core'
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { form, FormField, FormRoot, disabled, validate } from '@angular/forms/signals'
import { provideIcons } from '@ng-icons/core'
import { matCloseRound } from '@ng-icons/material-symbols/round'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { HlmAlertImports } from '@spartan-ng/helm/alert'
import { HlmCarouselImports } from '@spartan-ng/helm/carousel'
import { HlmDrawerImports } from '@spartan-ng/helm/drawer'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmTextarea } from '@spartan-ng/helm/textarea'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { firstValueFrom, map, take } from 'rxjs'

import {
	getMarkerErrorMessage,
	isMarkerOutcomeUncertain,
	MarkersQueryOptions,
	type CategoryGroup,
	type CreateMarkerRequest,
	type Marker,
	type MediaAsset,
	type UpdateMarkerRequest,
} from '@game-atlas/maps/data-access'
import { CategoryIcon, type MapPoint } from '@game-atlas/maps/ui'
import { Button, ConfirmationDialog, MediaPickerDialog, MediaThumbnail, Field } from '@game-atlas/shared/ui'
import { getDirtyValues, type LibraryImage, type PickerContext } from '@game-atlas/shared/util'

import { MarkerPanel } from './marker-panel'

type MarkerDraft = {
	name: string
	description: string
	categoryId: string
	position: MapPoint
	media: string[]
}

export type MarkerEditorSession = {
	marker: Marker | null
	position: MapPoint
	repeated: boolean
}

function createMarkerDraft(session: MarkerEditorSession, defaultCategoryId: string): MarkerDraft {
	const marker = session.marker

	return {
		name: marker?.name ?? '',
		description: marker?.description ?? '',
		categoryId: marker?.categoryId ?? defaultCategoryId,
		position: { ...session.position },
		media: marker?.media.map(asset => asset.id) ?? [],
	}
}

@Component({
	selector: 'maps-marker-editor',
	imports: [
		Field,
		CategoryIcon,
		HlmAlertImports,
		NgTemplateOutlet,
		Button,
		MediaThumbnail,
		FormField,
		FormRoot,
		HlmDrawerImports,
		HlmCarouselImports,
		HlmFieldImports,
		HlmInput,
		HlmTextarea,
		HlmSelectImports,
		MarkerPanel,
	],
	templateUrl: './marker-editor.html',
	providers: [provideIcons({ matCloseRound })],
})
export class MarkerEditor {
	readonly gameId = input.required<string>()
	readonly mapId = input.required<string>()
	readonly groups = input.required<CategoryGroup[]>()
	readonly session = input.required<MarkerEditorSession>()

	readonly saved = output<Marker>()
	readonly templateReady = output<CreateMarkerRequest>()
	readonly dismissed = output<void>()
	readonly boundsChanged = output<DOMRect>()

	readonly #dialogs = inject(BrnDialogService)
	readonly #options = inject(MarkersQueryOptions)
	readonly #destroy = inject(DestroyRef)
	readonly #breakpoints = inject(BreakpointObserver)

	protected readonly create = injectMutation(() => this.#options.create())
	protected readonly update = injectMutation(() => this.#options.update(this.markerId()))
	protected readonly remove = injectMutation(() => this.#options.delete(this.markerId()))

	#discardPromise: Promise<boolean> | undefined
	#resolveDiscard: ((allowed: boolean) => void) | undefined
	#lastBounds: DOMRect | undefined

	protected readonly desktopPanel = viewChild<ElementRef<HTMLElement>>('desktopPanel')

	protected readonly narrow = toSignal(
		this.#breakpoints.observe('(max-width: 767.98px)').pipe(map(value => value.matches)),
		{ initialValue: false },
	)

	protected readonly categories = computed(() => this.groups().flatMap(group => group.categories))
	protected readonly categoriesById = computed(
		() => new Map(this.categories().map(category => [category.id, category])),
	)
	protected readonly defaultCategoryId = computed(() => this.categories()[0]?.id ?? '')

	protected readonly marker = computed(() => this.session().marker)
	protected readonly markerId = computed(() => this.marker()?.id ?? 'new')
	protected readonly isEditing = computed(() => !!this.marker())
	protected readonly isRepeated = computed(() => this.session().repeated)

	protected readonly title = computed(() => {
		if (this.isEditing()) return 'Edit marker'

		return this.isRepeated() ? 'Marker template' : 'Add marker'
	})

	protected readonly draft = linkedSignal<MarkerEditorSession, MarkerDraft>({
		source: this.session,
		computation: session => createMarkerDraft(session, this.defaultCategoryId()),
	})

	protected readonly attachments = linkedSignal<MarkerEditorSession, MediaAsset[]>({
		source: this.session,
		computation: session => session.marker?.media ?? [],
	})

	protected readonly selectedCategory = computed(() => this.categoriesById().get(this.draft().categoryId))

	protected readonly error = computed(() => {
		const error = this.create.error() ?? this.update.error() ?? this.remove.error()

		return error ? getMarkerErrorMessage(error) : ''
	})

	protected readonly uncertain = computed(
		() => !!this.create.error() && isMarkerOutcomeUncertain(this.create.error()),
	)

	protected readonly discardRequested = signal(false)

	protected readonly fields = form(
		this.draft,
		path => {
			disabled(path, {
				when: () => this.create.isPending() || this.update.isPending() || this.remove.isPending(),
			})
			validate(path.name, ({ value }) => {
				const trimmed = value().trim()

				if (!trimmed) return { kind: 'required', message: 'Name is required.' }

				if (trimmed.length > 200) return { kind: 'maxLength', message: 'Use at most 200 characters.' }

				return undefined
			})
			validate(path.description, ({ value }) =>
				value().trim().length > 5000
					? { kind: 'maxLength', message: 'Use at most 5000 characters.' }
					: undefined,
			)
			validate(path.categoryId, ({ value }) =>
				this.categoriesById().has(value()) ? undefined : { kind: 'required', message: 'Choose a category.' },
			)
			validate(path.position.x, ({ value }) =>
				Number.isFinite(value()) ? undefined : { kind: 'finite', message: 'Enter a finite X coordinate.' },
			)
			validate(path.position.y, ({ value }) =>
				Number.isFinite(value()) ? undefined : { kind: 'finite', message: 'Enter a finite Y coordinate.' },
			)
		},
		{
			submission: {
				action: async fields => {
					const values = this.draft()
					const payload: CreateMarkerRequest = {
						...values,
						name: values.name.trim(),
						description: values.description.trim() || null,
					}

					if (this.isRepeated()) {
						fields().reset()
						this.templateReady.emit(payload)

						return
					}

					if (this.uncertain()) return

					const marker = this.marker()

					if (marker) {
						const dirty = getDirtyValues(fields)
						const patch: UpdateMarkerRequest = {
							...dirty,
							...(dirty.name === undefined ? {} : { name: payload.name }),
							...(dirty.description === undefined ? {} : { description: payload.description }),
						}

						if (!Object.keys(patch).length) return

						await this.update
							.mutateAsync(
								{ gameId: this.gameId(), mapId: this.mapId(), id: marker.id, ...patch },
								{
									onSuccess: result => {
										fields().reset()
										this.saved.emit(result)
									},
								},
							)
							.catch(() => undefined)
					} else {
						await this.create
							.mutateAsync(
								{ gameId: this.gameId(), mapId: this.mapId(), ...payload },
								{
									onSuccess: result => {
										fields().reset()
										this.saved.emit(result)
									},
								},
							)
							.catch(() => undefined)
					}
				},
			},
		},
	)

	readonly needsGuard = (): boolean => this.fields().dirty() || this.fields().submitting() || this.remove.isPending()

	readonly canLeave = async (): Promise<boolean> => {
		if (this.fields().submitting() || this.remove.isPending()) return false

		if (!this.fields().dirty()) return true

		if (this.#discardPromise) return this.#discardPromise

		this.discardRequested.set(true)
		this.#discardPromise = new Promise<boolean>(resolve => {
			this.#resolveDiscard = resolve
		})

		return this.#discardPromise
	}

	constructor() {
		afterRenderEffect(() => this.desktopPanel()?.nativeElement.focus({ preventScroll: true }))

		effect(() => {
			this.session()
			this.fields().reset()
			this.create.reset()
			this.update.reset()
			this.remove.reset()
			this.discardRequested.set(false)

			untracked(() => {
				if (!this.narrow()) {
					this.desktopPanel()?.nativeElement.focus({ preventScroll: true })
				} else if (this.#lastBounds) {
					this.boundsChanged.emit(this.#lastBounds)
				}
			})
		})

		this.#destroy.onDestroy(() => this.#resolveDiscard?.(false))
	}

	protected categoryName = (value: unknown): string =>
		this.categoriesById().get(value as string)?.name ?? 'Choose a category'

	protected resolveDiscard(allowed: boolean): void {
		this.discardRequested.set(false)
		if (allowed) this.fields().reset()

		this.#resolveDiscard?.(allowed)
		this.#resolveDiscard = undefined
		this.#discardPromise = undefined
	}

	protected handleBoundsChanged(bounds: DOMRect): void {
		this.#lastBounds = bounds
		this.boundsChanged.emit(bounds)
	}

	protected async close(): Promise<void> {
		if (!(await this.canLeave())) return

		if (!this.narrow()) this.desktopPanel()?.nativeElement.ownerDocument.getElementById('map-marker-tools')?.focus()

		this.dismissed.emit()
	}

	protected choosePhoto(): void {
		this.#dialogs
			.open<PickerContext, LibraryImage>(
				MediaPickerDialog,
				undefined,
				{ mode: 'image' },
				{ ariaLabel: 'Choose marker photo', restoreFocus: '#marker-photo' },
			)
			.closed$.pipe(take(1), takeUntilDestroyed(this.#destroy))
			.subscribe(image => {
				if (!image || this.draft().media.includes(image.id)) return

				this.attachments.update(assets => [...assets, image])
				this.draft.update(value => ({ ...value, media: [...value.media, image.id] }))
				this.fields.media().markAsDirty()
			})
	}

	protected detach(id: string): void {
		this.attachments.update(assets => assets.filter(asset => asset.id !== id))
		this.draft.update(value => ({ ...value, media: value.media.filter(assetId => assetId !== id) }))
		this.fields.media().markAsDirty()
	}

	protected reorder(index: number, delta: number): void {
		const assets = this.attachments().slice()
		const imageIndexes = assets.flatMap((asset, index) => (asset.type === 'image' ? [index] : []))
		const target = imageIndexes[imageIndexes.indexOf(index) + delta]

		if (target === undefined) return

		;[assets[index], assets[target]] = [assets[target], assets[index]]
		this.attachments.set(assets)
		this.draft.update(value => ({ ...value, media: assets.map(asset => asset.id) }))
		this.fields.media().markAsDirty()
	}

	protected async deleteMarker(): Promise<void> {
		const marker = this.marker()

		if (!marker) return

		const confirmed = await firstValueFrom(
			this.#dialogs
				.open(
					ConfirmationDialog,
					undefined,
					{
						title: `Delete “${marker.name}”?`,
						description: 'This deletes the marker. Library photos and videos are kept.',
						confirm: 'Delete',
						cancel: 'Cancel',
						destructive: true,
					},
					{ ariaLabel: 'Delete marker?', role: 'alertdialog', closeOnOutsidePointerEvents: false },
				)
				.closed$.pipe(take(1)),
		)

		if (confirmed !== true) return

		await this.remove
			.mutateAsync(
				{ gameId: this.gameId(), mapId: this.mapId(), id: marker.id },
				{
					onSuccess: () => {
						this.fields().reset()
						this.dismissed.emit()
					},
				},
			)
			.catch(() => undefined)
	}
}
