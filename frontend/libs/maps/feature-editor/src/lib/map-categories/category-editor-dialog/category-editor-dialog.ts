import { CategoryIcon } from '@game-atlas/maps/ui'
import { Component, computed, DestroyRef, inject, signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { disabled, form, FormField, FormRoot, maxLength, pattern, required } from '@angular/forms/signals'
import {
	CategoriesQueryOptions,
	type Category,
	type CategoryGroup,
	getCategoryErrorMessage,
} from '@game-atlas/maps/data-access'
import { Button, ColorPicker, MediaPickerDialog, MediaThumbnail, Field } from '@game-atlas/shared/ui'
import { getDirtyValues, type LibraryImage, type PickerContext } from '@game-atlas/shared/util'
import { BrnDialogRef, BrnDialogService, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { HlmBadge } from '@spartan-ng/helm/badge'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmSwitchImports } from '@spartan-ng/helm/switch'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matArrowDownwardRound,
	matArrowUpwardRound,
	matChevronRightRound,
	matCreateNewFolderRound,
	matDeleteRound,
	matDragIndicatorRound,
	matDriveFileMoveRound,
	matEditRound,
	matFolderRound,
	matFormatColorResetRound,
	matMoreHorizRound,
} from '@ng-icons/material-symbols/round'

export type CategoryEditorContext = {
	gameId: string
	mapId: string
	groups: CategoryGroup[]
	kind: 'group' | 'category'
	group: CategoryGroup | null
	category: Category | null
}

@Component({
	selector: 'maps-category-editor-dialog',
	imports: [
		Field,
		MediaThumbnail,
		CategoryIcon,
		Button,
		HlmBadge,
		HlmFieldImports,
		HlmInput,
		HlmSelectImports,
		HlmSwitchImports,
		FormField,
		FormRoot,
		ColorPicker,
		HlmInputGroupImports,
		NgIcon,
	],
	providers: [
		provideIcons({
			matEditRound,
			matDeleteRound,
			matArrowUpwardRound,
			matArrowDownwardRound,
			matDriveFileMoveRound,
			matFolderRound,
			matAddRound,
			matMoreHorizRound,
			matFormatColorResetRound,
			matDragIndicatorRound,
			matChevronRightRound,
			matCreateNewFolderRound,
		}),
	],
	templateUrl: './category-editor-dialog.html',
	host: {
		class: 'block max-h-[calc(100dvh-2rem)] w-[min(30rem,calc(100vw-2rem))] overflow-auto rounded-4xl bg-popover p-6 text-popover-foreground ring-1 ring-foreground/5',
	},
})
export class CategoryEditorDialog {
	protected readonly context = injectBrnDialogContext<CategoryEditorContext>()
	protected readonly dialog = inject(BrnDialogRef)

	readonly #options = inject(CategoriesQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #destroy = inject(DestroyRef)

	readonly #create = injectMutation(() => this.#options.create())
	readonly #update = injectMutation(() => this.#options.update())
	readonly #createGroup = injectMutation(() => this.#options.createGroup())
	readonly #updateGroup = injectMutation(() => this.#options.updateGroup())

	protected readonly existing = this.context.kind === 'group' ? this.context.group : this.context.category

	protected readonly title =
		(this.existing ? 'Edit ' : 'Add ') + (this.context.kind === 'group' ? 'group' : 'category')

	protected readonly groupName = (value: unknown): string =>
		this.context.groups.find(group => group.id === value)?.name ?? 'Choose a group'

	protected readonly icon = signal<LibraryImage | null>(this.existing?.icon ?? null)

	readonly #model = signal({
		name: this.existing?.name ?? '',
		iconAssetId: this.existing?.icon?.id ?? (null as string | null),
		color: this.context.kind === 'group' ? (this.existing?.color ?? '#F59E0B') : (this.existing?.color ?? ''),
		categoryGroupId:
			this.context.category?.categoryGroupId ?? this.context.group?.id ?? this.context.groups[0]?.id ?? '',
		published: this.existing?.status === 'published',
	})

	protected readonly selectedGroup = computed(() =>
		this.context.groups.find(group => group.id === this.#model().categoryGroupId),
	)

	protected readonly fields = form(
		this.#model,
		path => {
			disabled(path, {
				when: () =>
					this.#create.isPending() ||
					this.#update.isPending() ||
					this.#createGroup.isPending() ||
					this.#updateGroup.isPending(),
			})

			required(path.name, { message: 'Name is required.' })
			pattern(path.name, /\S/, { message: 'Name is required.' })
			maxLength(path.name, 200, { message: 'Use at most 200 characters.' })

			required(path.color, {
				when: () => this.context.kind === 'group',
				message: 'Use a hex color such as #FFF or #FFFFFF.',
			})
			pattern(path.color, /^#(?:[a-f\d]{3}|[a-f\d]{6})(?![\s\S])/i, {
				message: 'Use a hex color such as #FFF or #FFFFFF.',
			})

			required(path.categoryGroupId, {
				when: () => this.context.kind === 'category',
				message: 'Choose a category group.',
			})
		},
		{
			submission: {
				action: async fields => {
					const { gameId, mapId, kind, group, category } = this.context
					const values = this.#model()
					const callbacks = {
						onSuccess: () => {
							fields().reset()
							this.dialog.close(true)
						},
					}

					if (kind === 'group') {
						if (group) {
							const { name, iconAssetId, color, published } = getDirtyValues(fields)
							const nextStatus =
								published === undefined
									? undefined
									: published
										? ('published' as const)
										: ('draft' as const)
							const patch = {
								...(name === undefined ? {} : { name: name.trim() }),
								...(iconAssetId === undefined ? {} : { iconAssetId }),
								...(color === undefined ? {} : { color }),
								...(nextStatus !== undefined && nextStatus !== group.status
									? { status: nextStatus }
									: {}),
							}

							if (!Object.keys(patch).length) return

							await this.#updateGroup
								.mutateAsync({ gameId, mapId, id: group.id, ...patch }, callbacks)
								.catch(() => undefined)
						} else {
							await this.#createGroup
								.mutateAsync(
									{
										gameId,
										mapId,
										name: values.name.trim(),
										iconAssetId: values.iconAssetId,
										color: values.color,
										status: values.published ? 'published' : 'draft',
									},
									callbacks,
								)
								.catch(() => undefined)
						}
					} else if (category) {
						const { published, ...dirty } = getDirtyValues(fields)
						const nextStatus =
							published === undefined
								? undefined
								: published
									? ('published' as const)
									: ('draft' as const)
						const patch = {
							...dirty,
							...(dirty.name === undefined ? {} : { name: dirty.name.trim() }),
							...(dirty.color === undefined ? {} : { color: dirty.color.trim() || null }),
							...(nextStatus !== undefined && nextStatus !== category.status
								? { status: nextStatus }
								: {}),
						}

						if (!Object.keys(patch).length) return

						await this.#update
							.mutateAsync(
								{
									gameId,
									mapId,
									groupId: category.categoryGroupId,
									id: category.id,
									...patch,
								},
								callbacks,
							)
							.catch(() => undefined)
					} else {
						await this.#create
							.mutateAsync(
								{
									gameId,
									mapId,
									groupId: values.categoryGroupId,
									name: values.name.trim(),
									color: values.color.trim() || null,
									iconAssetId: values.iconAssetId,
									status: values.published ? 'published' : 'draft',
								},
								callbacks,
							)
							.catch(() => undefined)
					}
				},
			},
		},
	)

	protected readonly error = computed(() => {
		const error =
			this.#create.error() ?? this.#update.error() ?? this.#createGroup.error() ?? this.#updateGroup.error()

		return error ? getCategoryErrorMessage(error) : ''
	})

	protected chooseIcon(): void {
		this.#dialogs
			.open<PickerContext, LibraryImage>(
				MediaPickerDialog,
				undefined,
				{ mode: 'image' },
				{ ariaLabel: 'Choose category icon', restoreFocus: '#category-icon' },
			)
			.closed$.pipe(take(1), takeUntilDestroyed(this.#destroy))
			.subscribe(image => {
				if (!image) return

				this.icon.set(image)
				this.#model.update(value => ({ ...value, iconAssetId: image.id }))
				this.fields.iconAssetId().markAsDirty()
			})
	}

	protected clearIcon(): void {
		this.icon.set(null)
		this.#model.update(value => ({ ...value, iconAssetId: null }))
		this.fields.iconAssetId().markAsDirty()
	}
}
