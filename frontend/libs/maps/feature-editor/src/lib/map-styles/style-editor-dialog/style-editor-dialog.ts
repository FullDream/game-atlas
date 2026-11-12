import { Component, computed, DestroyRef, inject, signal } from '@angular/core'
import { disabled, form, FormField, FormRoot, maxLength, required, validate } from '@angular/forms/signals'
import {
	MapsQueryOptions,
	getMapStyleErrorMessage,
	type MapStyle,
	type Map as GameMap,
} from '@game-atlas/maps/data-access'
import { Button, ColorPicker, Field, MediaPickerDialog, MediaThumbnail } from '@spartan-ng/helm'
import { getDirtyValues, type LibraryImage, type PickerContext } from '@game-atlas/shared/util'
import { BrnDialogRef, BrnDialogService, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { take } from 'rxjs'
import { HlmInputGroup, HlmInputGroupAddon, HlmInputGroupButton } from '@spartan-ng/helm/input-group'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matFormatColorResetRound } from '@ng-icons/material-symbols/round'

type StyleEditorContext = { gameId: string; map: GameMap; style: MapStyle | null }
type StyleForm = { name: string; backgroundColor: string; sourceAssetId: string }

@Component({
	selector: 'maps-style-editor-dialog',
	imports: [Button, MediaThumbnail, HlmFieldImports, HlmInput, FormField, FormRoot, ColorPicker, Field],
	templateUrl: './style-editor-dialog.html',
	providers: [provideIcons({ matFormatColorResetRound })],
	host: {
		class: 'block max-h-[calc(100dvh-2rem)] w-[min(30rem,calc(100vw-2rem))] overflow-auto rounded-4xl bg-popover p-6 text-popover-foreground ring-1 ring-foreground/5',
	},
})
export class StyleEditorDialog {
	protected readonly context = injectBrnDialogContext<StyleEditorContext>()
	protected readonly dialog = inject(BrnDialogRef)
	readonly #options = inject(MapsQueryOptions)
	readonly #dialogs = inject(BrnDialogService)
	readonly #destroy = inject(DestroyRef)

	readonly #create = injectMutation(() => this.#options.addStyle(this.context.gameId, this.context.map.id))
	readonly #update = injectMutation(() => this.#options.updateStyle(this.context.gameId, this.context.map.id))

	protected readonly source = signal<LibraryImage | null>(this.context.style?.source ?? null)

	readonly #model = signal<StyleForm>({
		name: this.context.style?.name ?? '',
		backgroundColor: this.context.style?.backgroundColor ?? '#FFFFFF',
		sourceAssetId: this.context.style?.source.id ?? '',
	})

	protected readonly fields = form(
		this.#model,
		path => {
			disabled(path, { when: () => this.#create.isPending() || this.#update.isPending() })

			required(path.name, { message: 'Style name is required' })
			maxLength(path.name, 200)

			validate(path.backgroundColor, ({ value }) =>
				/^#(?:[a-f\d]{3}|[a-f\d]{6})$/i.test(value())
					? undefined
					: { kind: 'color', message: 'Use a hex color such as #FFF or #FFFFFF' },
			)

			required(path.sourceAssetId)

			validate(path.sourceAssetId, () => {
				if (this.context.style) return undefined

				const source = this.source()

				if (source && !['image/jpeg', 'image/png', 'image/webp', 'image/avif'].includes(source.contentType))
					return { kind: 'format', message: 'Use a JPEG, PNG, WebP or AVIF image.' }

				return undefined
			})
		},
		{
			submission: {
				action: async fields => {
					const callbacks = {
						onSuccess: () => {
							fields().reset()
							this.dialog.close(true)
						},
					}

					if (this.context.style) {
						const { name, backgroundColor } = getDirtyValues(fields)

						const patch = {
							...(name === undefined ? {} : { name: name.trim() }),
							...(backgroundColor === undefined ? {} : { backgroundColor }),
						}

						if (!Object.keys(patch).length) return

						await this.#update
							.mutateAsync({ styleId: this.context.style.id, patch }, callbacks)
							.catch(() => undefined)
					} else {
						await this.#create
							.mutateAsync(
								{
									mapId: this.context.map.id,
									data: { ...this.#model(), name: this.#model().name.trim() },
								},
								callbacks,
							)
							.catch(() => undefined)
					}
				},
			},
		},
	)

	protected readonly aspectRatioMismatch = computed(() => {
		const source = this.source()
		const { width, height } = this.context.map

		return !!(source?.width && source.height && width && height && source.width * height !== source.height * width)
	})

	protected readonly error = computed(() => {
		const error = this.context.style ? this.#update.error() : this.#create.error()

		return error ? getMapStyleErrorMessage(error) : ''
	})

	protected chooseImage(): void {
		this.#dialogs
			.open<PickerContext, LibraryImage>(
				MediaPickerDialog,
				undefined,
				{ mode: 'image' },
				{ ariaLabel: 'Choose style image', restoreFocus: '#style-source' },
			)
			.closed$.pipe(take(1), takeUntilDestroyed(this.#destroy))
			.subscribe(image => {
				if (!image) return

				this.source.set(image)
				this.#model.update(model => ({ ...model, sourceAssetId: image.id }))
				this.fields.sourceAssetId().markAsDirty()
				this.fields.sourceAssetId().markAsTouched()
			})
	}
}
