import { Component, computed, inject, signal } from '@angular/core'
import {
	disabled,
	form,
	FormField,
	FormRoot,
	min,
	max,
	readonly as readOnlyField,
	validate,
} from '@angular/forms/signals'
import { MapsQueryOptions, getMapErrorMessage, type Map as GameMap } from '@game-atlas/maps/data-access'
import { Button } from '@game-atlas/shared/ui'
import { getDirtyValues } from '@game-atlas/shared/util'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { injectMutation } from '@tanstack/angular-query-experimental'

type MapEditorContext = { gameId: string; map: GameMap | null }
type MapForm = { name: string; width: number; height: number }

@Component({
	selector: 'maps-editor-dialog',
	imports: [Button, HlmFieldImports, HlmInput, FormField, FormRoot],
	templateUrl: './map-editor-dialog.html',
	host: {
		'[attr.data-state]': 'dialog.state()',
		style: 'max-height: calc(100dvh - 2rem)',
		class: 'block w-[min(28rem,calc(100vw-2rem))] overflow-auto rounded-xl border bg-background p-6 text-foreground shadow-xl',
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
		width: this.context.map?.width ?? 4096,
		height: this.context.map?.height ?? 4096,
	})

	protected readonly fields = form(
		this.#model,
		path => {
			disabled(path, { when: () => this.#create.isPending() || this.#update.isPending() })
			validate(path.name, ({ value }) => {
				const name = value().trim()

				if (!name) return { kind: 'required', message: 'Map name is required.' }

				if (name.length > 200) return { kind: 'maxLength', message: 'Name must be at most 200 characters.' }

				return undefined
			})
			for (const dimension of [path.width, path.height]) {
				readOnlyField(dimension, { when: () => !!this.context.map })
				min(dimension, 1, { message: 'Use at least 1 pixel.' })
				max(dimension, 16384, { message: 'Use at most 16,384 pixels.' })
				validate(dimension, ({ value }) =>
					Number.isInteger(value())
						? undefined
						: { kind: 'integer', message: 'Enter a whole number of pixels.' },
				)
			}
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
						const { name } = getDirtyValues(fields)

						if (name === undefined || name.trim() === this.context.map.name) return

						await this.#update
							.mutateAsync({ id: this.context.map.id, patch: { name: name.trim() } }, callbacks)
							.catch(() => undefined)
					} else {
						await this.#create
							.mutateAsync({ ...this.#model(), name: this.#model().name.trim() }, callbacks)
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
