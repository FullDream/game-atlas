import { CategoryIcon } from '@game-atlas/maps/ui'
import { Component, computed, DestroyRef, inject, signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { disabled, form, FormField, FormRoot, validate } from '@angular/forms/signals'
import {
	CategoriesQueryOptions,
	type Category,
	type CategoryGroup,
	getCategoryErrorMessage,
} from '@game-atlas/maps/data-access'
import { Button, ColorPicker, MediaPickerDialog, MediaThumbnail, Field } from '@game-atlas/shared/ui'
import { getDirtyValues, type LibraryImage, type PickerContext } from '@game-atlas/shared/util'
import { BrnDialogRef, BrnDialogService, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { take } from 'rxjs'
import { HlmButtonGroupImports } from '@spartan-ng/helm/button-group'
import { HlmButtonImports } from '@spartan-ng/helm/button'
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
		HlmFieldImports,
		HlmInput,
		HlmSelectImports,
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
			validate(path.name, ({ value }) => {
				const name = value().trim()

				if (!name) return { kind: 'required', message: 'Name is required.' }

				if (name.length > 200) return { kind: 'maxLength', message: 'Use at most 200 characters.' }

				return undefined
			})
			validate(path.color, ({ value }) =>
				(this.context.kind === 'category' && !value().trim()) ||
				/^#(?:[a-f\d]{3}|[a-f\d]{6})(?![\s\S])/i.test(value())
					? undefined
					: { kind: 'color', message: 'Use a hex color such as #FFF or #FFFFFF.' },
			)
			validate(path.categoryGroupId, ({ value }) =>
				this.context.kind === 'group' || value()
					? undefined
					: { kind: 'required', message: 'Choose a category group.' },
			)
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
							const { name, iconAssetId, color } = getDirtyValues(fields)
							const patch = {
								...(name === undefined ? {} : { name: name.trim() }),
								...(iconAssetId === undefined ? {} : { iconAssetId }),
								...(color === undefined ? {} : { color }),
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
									},
									callbacks,
								)
								.catch(() => undefined)
						}
					} else if (category) {
						const patch = getDirtyValues(fields)

						if (!Object.keys(patch).length) return

						await this.#update
							.mutateAsync(
								{
									gameId,
									mapId,
									groupId: category.categoryGroupId,
									id: category.id,
									...patch,
									...(patch.name === undefined ? {} : { name: patch.name.trim() }),
									...(patch.color === undefined ? {} : { color: patch.color.trim() || null }),
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
