import { isPlatformBrowser } from '@angular/common'
import { DOCUMENT, effect, inject, PLATFORM_ID, REQUEST, Service, signal } from '@angular/core'
import { Meta } from '@angular/platform-browser'
import type { ThemePreference } from '@game-atlas/shared/ui'

const THEME_COOKIE_NAME = 'game-atlas.theme'
const THEME_COOKIE_MAX_AGE = 31_536_000

function parseThemePreference(raw: string | null | undefined): ThemePreference {
	return raw === 'light' || raw === 'dark' ? raw : 'system'
}

@Service()
export class ThemeSettings {
	readonly #request = inject(REQUEST, { optional: true })
	readonly #document = inject(DOCUMENT)
	readonly #meta = inject(Meta)
	readonly #platformId = inject(PLATFORM_ID)

	readonly preference = signal<ThemePreference>(this.#readInitialPreference())

	constructor() {
		this.#applyMeta(this.preference())

		let isFirstRun = true

		effect(() => {
			const preference = this.preference()

			this.#applyMeta(preference)

			if (!isFirstRun && isPlatformBrowser(this.#platformId)) {
				this.#writePreference(preference)
			}

			isFirstRun = false
		})
	}

	#readInitialPreference(): ThemePreference {
		if (!isPlatformBrowser(this.#platformId)) {
			return parseThemePreference(this.#readCookie(this.#request?.headers.get('cookie')))
		}

		const fromCookie = this.#readCookie(this.#document.cookie)

		if (fromCookie !== null) {
			return parseThemePreference(fromCookie)
		}

		try {
			return parseThemePreference(globalThis.localStorage?.getItem(THEME_COOKIE_NAME))
		} catch {
			return 'system'
		}
	}

	#readCookie(cookieHeader: string | null | undefined): string | null {
		if (!cookieHeader) return null

		for (const part of cookieHeader.split(';')) {
			const separatorIndex = part.indexOf('=')

			if (separatorIndex === -1) continue

			const name = part.slice(0, separatorIndex).trim()

			if (name !== THEME_COOKIE_NAME) continue

			const rawValue = part.slice(separatorIndex + 1).trim()

			try {
				return decodeURIComponent(rawValue)
			} catch {
				return rawValue
			}
		}

		return null
	}

	#applyMeta(preference: ThemePreference): void {
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
	}

	#writePreference(preference: ThemePreference): void {
		this.#document.cookie = `${THEME_COOKIE_NAME}=${encodeURIComponent(preference)}; Path=/; Max-Age=${THEME_COOKIE_MAX_AGE}; SameSite=Lax`

		try {
			globalThis.localStorage?.setItem(THEME_COOKIE_NAME, preference)
		} catch {
			// Ignore storage access errors
		}
	}
}
