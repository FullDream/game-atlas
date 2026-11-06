import { Component, computed, inject, input, linkedSignal, model } from '@angular/core'
import { disabled, form, FormField, FormRoot, validate } from '@angular/forms/signals'
import { getMapErrorMessage, MapsQueryOptions, type Map as GameMap } from '@game-atlas/maps/data-access'
import { Button } from '@game-atlas/shared/ui'
import { getDirtyValues } from '@game-atlas/shared/util'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matSaveRound, matSettingsRound } from '@ng-icons/material-symbols/round'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { injectMutation } from '@tanstack/angular-query-experimental'

type MapNameForm = { name: string }

@Component({
	selector: 'maps-settings',
	imports: [
		Button,
		NgIcon,
		FormField,
		FormRoot,
		HlmAccordionImports,
		HlmFieldImports,
		HlmInputGroupImports,
		HlmTooltip,
	],
	templateUrl: './map-settings.html',
	providers: [provideIcons({ matSaveRound, matSettingsRound })],
	host: {
		class: 'flex min-h-10 basis-10 shrink-0 flex-col overflow-hidden transition-[flex-grow] duration-200 motion-reduce:transition-none',
		'[style.flex-grow]': 'sectionOpen() ? 1 : 0',
	},
})
export class MapSettings {
	readonly gameId = input.required<string>()
	readonly map = input.required<GameMap>()
	readonly sectionOpen = model(false)

	readonly #options = inject(MapsQueryOptions)

	protected readonly update = injectMutation(() => this.#options.update(this.gameId(), this.map().id))

	protected readonly nameModel = linkedSignal<GameMap, MapNameForm>({
		source: this.map,
		computation: (map, previous) => (previous?.source.id === map.id ? previous.value : { name: map.name }),
	})

	protected readonly fields = form(
		this.nameModel,
		path => {
			disabled(path, { when: () => this.update.isPending() })
			validate(path.name, ({ value }) => {
				const name = value().trim()

				if (!name) return { kind: 'required', message: 'Map name is required.' }

				if (name.length > 200) return { kind: 'maxLength', message: 'Name must be at most 200 characters.' }

				return undefined
			})
		},
		{
			submission: {
				action: async fields => {
					const { name } = getDirtyValues(fields)

					if (name === undefined || name.trim() === this.map().name) return

					const mapId = this.map().id

					await this.update
						.mutateAsync(
							{ id: mapId, patch: { name: name.trim() } },
							{
								onSuccess: map => {
									if (this.map().id !== mapId) return

									this.nameModel.set({ name: map.name })
									fields().reset()
								},
							},
						)
						.catch(() => undefined)
				},
			},
		},
	)

	protected readonly error = computed(() => (this.update.error() ? getMapErrorMessage(this.update.error()) : ''))
}
