import { Component, computed, inject, signal } from '@angular/core'
import { disabled, form, FormField, FormRoot, maxLength, pattern, required } from '@angular/forms/signals'
import { MapsQueryOptions, getMapErrorMessage, type Map as GameMap } from '@game-atlas/maps/data-access'
import { Button, Field } from '@game-atlas/shared/ui'
import { getDirtyValues } from '@game-atlas/shared/util'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmSwitchImports } from '@spartan-ng/helm/switch'
import { injectMutation } from '@tanstack/angular-query'

type MapEditorContext = { gameId: string; map: GameMap | null }
type MapForm = { name: string; slug: string; published: boolean }

@Component({
	selector: 'maps-editor-dialog',
	imports: [Field, Button, HlmFieldImports, HlmInput, HlmSwitchImports, FormField, FormRoot],
	templateUrl: './map-editor-dialog.html',
	host: {
		'[attr.data-state]': 'dialog.state()',
		style: 'max-height: calc(100dvh - 2rem)',
		class: 'block w-[min(28rem,calc(100vw-2rem))] overflow-auto rounded-4xl bg-popover p-6 text-popover-foreground ring-1 ring-foreground/5',
	},
})
export class MapEditorDialog {
	protected readonly context = injectBrnDialogContext<MapEditorContext>()
	protected readonly dialog = inject(BrnDialogRef)
	readonly #options = inject(MapsQueryOptions)

	readonly #create = injectMutation(() => this.#options.create(this.context.gameId))
	readonly #update = injectMutation(() => this.#options.update(this.context.gameId, this.context.map?.id ?? ''))

	readonly #model = signal<MapForm>({
		name: this.context.map?.name ?? '',
		slug: this.context.map?.slug ?? '',
		published: this.context.map?.status === 'published',
	})

	protected readonly fields = form(
		this.#model,
		path => {
			disabled(path, { when: () => this.#create.isPending() || this.#update.isPending() })

			required(path.name, { message: 'Map name is required.' })
			pattern(path.name, /\S/, { message: 'Map name is required.' })
			maxLength(path.name, 200, { message: 'Name must be at most 200 characters.' })

			required(path.slug, { message: 'Map slug is required.' })
			maxLength(path.slug, 200, { message: 'Slug must be at most 200 characters.' })
			pattern(path.slug, /^[a-z0-9]+(-[a-z0-9]+)*$/, {
				message: 'Use lowercase ASCII letters or digits separated by single hyphens.',
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

					if (this.context.map) {
						const { name, slug, published } = getDirtyValues(fields)
						const nextStatus =
							published === undefined
								? undefined
								: published
									? ('published' as const)
									: ('draft' as const)

						const patch = {
							...(name !== undefined && name.trim() !== this.context.map.name
								? { name: name.trim() }
								: {}),
							...(slug !== undefined && slug.trim() !== this.context.map.slug
								? { slug: slug.trim() }
								: {}),
							...(nextStatus !== undefined && nextStatus !== this.context.map.status
								? { status: nextStatus }
								: {}),
						}

						if (!Object.keys(patch).length) return

						await this.#update
							.mutateAsync({ id: this.context.map.id, patch }, callbacks)
							.catch(() => undefined)
					} else {
						const model = this.#model()

						await this.#create
							.mutateAsync(
								{
									name: model.name.trim(),
									slug: model.slug.trim(),
									status: model.published ? 'published' : 'draft',
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
		const error = this.context.map ? this.#update.error() : this.#create.error()

		return error ? getMapErrorMessage(error) : ''
	})
}
