import { isPlatformServer } from '@angular/common'
import {
	afterNextRender,
	computed,
	DestroyRef,
	DOCUMENT,
	inject,
	Injectable,
	PLATFORM_ID,
	REQUEST,
	type Signal,
	signal,
} from '@angular/core'
import { injectHlmSidebarConfig } from './hlm-sidebar.token'

export type SidebarVariant = 'sidebar' | 'floating' | 'inset'

@Injectable({ providedIn: 'root' })
export class HlmSidebarService {
	readonly #platformId = inject(PLATFORM_ID)
	readonly #request = inject(REQUEST, { optional: true })
	readonly #config = injectHlmSidebarConfig()
	readonly #document = inject(DOCUMENT)
	readonly #window = this.#document.defaultView
	readonly #open = signal<boolean>(this.#config.defaultOpen)
	readonly #openMobile = signal<boolean>(false)
	readonly #isMobile = signal<boolean>(false)
	readonly #variant = signal<SidebarVariant>('sidebar')
	#mediaQuery: MediaQueryList | null = null

	public readonly open: Signal<boolean> = this.#open.asReadonly()
	public readonly openMobile: Signal<boolean> = this.#openMobile.asReadonly()
	public readonly isMobile: Signal<boolean> = this.#isMobile.asReadonly()
	public readonly variant: Signal<SidebarVariant> = this.#variant.asReadonly()

	public readonly state = computed<'expanded' | 'collapsed'>(() => (this.#open() ? 'expanded' : 'collapsed'))

	constructor() {
		const destroyRef = inject(DestroyRef)

		this.#restoreStateFromCookie()

		afterNextRender(() => {
			if (!this.#window || typeof this.#window.matchMedia !== 'function') return

			// Initialize MediaQueryList
			this.#mediaQuery = this.#window.matchMedia(`(max-width: ${this.#config.mobileBreakpoint})`)
			this.#isMobile.set(this.#mediaQuery.matches)

			// Add media query listener
			const mediaQueryHandler = (e: MediaQueryListEvent): void => {
				this.#isMobile.set(e.matches)
				// If switching from mobile to desktop, close mobile sidebar
				if (!e.matches) this.#openMobile.set(false)
			}

			this.#mediaQuery.addEventListener('change', mediaQueryHandler)

			// Add keyboard shortcut listener
			const keydownHandler = (event: KeyboardEvent): void => {
				if (event.key === this.#config.sidebarKeyboardShortcut && (event.ctrlKey || event.metaKey)) {
					event.preventDefault()
					this.toggleSidebar()
				}
			}

			this.#window.addEventListener('keydown', keydownHandler)

			// Add resize listener with debounce
			let resizeTimeout: number
			const resizeHandler = (): void => {
				if (!this.#window) return

				if (resizeTimeout) this.#window.clearTimeout(resizeTimeout)

				resizeTimeout = this.#window.setTimeout(() => {
					if (this.#mediaQuery) this.#isMobile.set(this.#mediaQuery.matches)
				}, 100)
			}

			this.#window.addEventListener('resize', resizeHandler)

			// Cleanup listeners on destroy
			destroyRef.onDestroy(() => {
				if (!this.#window) return

				if (this.#mediaQuery) this.#mediaQuery.removeEventListener('change', mediaQueryHandler)

				this.#window.removeEventListener('keydown', keydownHandler)
				this.#window.removeEventListener('resize', resizeHandler)
				if (resizeTimeout) this.#window.clearTimeout(resizeTimeout)
			})
		})
	}

	public setOpen(open: boolean): void {
		this.#open.set(open)
		this.#document.cookie = `${this.#config.sidebarCookieName}=${open}; path=/; max-age=${this.#config.sidebarCookieMaxAge}`
	}

	public setOpenMobile(open: boolean): void {
		if (this.#isMobile()) {
			this.#openMobile.set(open)
		}
	}

	public setVariant(variant: SidebarVariant): void {
		this.#variant.set(variant)
	}

	public toggleSidebar(): void {
		if (this.#isMobile()) {
			this.#openMobile.update(value => !value)
		} else {
			this.setOpen(!this.#open())
		}
	}

	#restoreStateFromCookie(): void {
		const cookieString = isPlatformServer(this.#platformId)
			? this.#request?.headers.get('cookie')
			: this.#document.cookie

		if (!cookieString) return

		const prefix = `${this.#config.sidebarCookieName}=`
		const cookieValue = cookieString
			.split(';')
			.map(c => c.trim())
			.find(c => c.startsWith(prefix))
			?.slice(prefix.length)

		if (cookieValue !== undefined) {
			this.#open.set(cookieValue === 'true')
		}
	}
}
