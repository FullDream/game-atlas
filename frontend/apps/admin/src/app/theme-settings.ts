import { afterRenderEffect, inject, Service } from '@angular/core'
import { Meta } from '@angular/platform-browser'
import type { ThemePreference } from '@game-atlas/shared/ui'
import { injectLocalStorage } from 'ngxtension/inject-local-storage'

@Service()
export class ThemeSettings {
	readonly #meta = inject(Meta)

	readonly preference = injectLocalStorage<ThemePreference>('game-atlas.theme', {
		defaultValue: 'system',
		parse: value => (value === 'light' || value === 'dark' ? value : 'system'),
		stringify: value => value,
	})

	constructor() {
		afterRenderEffect(() => {
			const preference = this.preference()

			this.#meta.updateTag({
				name: 'color-scheme',
				content: preference === 'system' ? 'light dark' : preference,
			})

			for (const scheme of ['light', 'dark']) {
				let media = 'not all'

				if (preference === 'system') {
					media = `(prefers-color-scheme: ${scheme})`
				} else if (preference === scheme) {
					media = 'all'
				}

				this.#meta.getTag(`id='theme-color-${scheme}'`)?.setAttribute('media', media)
			}
		})
	}
}
