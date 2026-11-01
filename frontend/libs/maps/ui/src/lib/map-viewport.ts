import {
	afterNextRender,
	afterRenderEffect,
	Component,
	computed,
	DestroyRef,
	ElementRef,
	inject,
	input,
	model,
	signal,
	viewChild,
	ViewEncapsulation,
} from '@angular/core'
import { FormsModule } from '@angular/forms'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matRemoveRound,
	matFullscreenRound,
	matFullscreenExitRound,
	matFitScreenRound,
} from '@ng-icons/material-symbols/round'
import type { MapEngine } from './map-engine'
import type { MapGeometry, MapViewStyle } from './map-viewport-types'

@Component({
	selector: 'maps-viewport',
	imports: [Button, MediaThumbnail, FormsModule, HlmSelectImports, HlmEmptyImports, HlmTooltip],
	templateUrl: './map-viewport.html',
	styleUrl: './map-viewport.css',
	encapsulation: ViewEncapsulation.None,
	providers: [
		provideIcons({ matAddRound, matRemoveRound, matFullscreenRound, matFullscreenExitRound, matFitScreenRound }),
	],
	host: {
		class: 'relative block min-h-0 min-w-0 overflow-hidden bg-muted',
		'[style.background-color]': 'activeStyle()?.backgroundColor',
	},
})
export class MapViewport {
	readonly geometry = input.required<MapGeometry>()
	readonly styles = input.required<MapViewStyle[]>()
	readonly defaultStyleId = input<string | null>(null)
	readonly selectedStyleId = model<string | null>(null)

	readonly #host = inject(ElementRef<HTMLElement>)
	readonly #destroy = inject(DestroyRef)

	protected readonly canvas = viewChild.required<ElementRef<HTMLElement>>('canvas')
	protected readonly fullscreenAvailable = signal(false)
	protected readonly fullscreen = signal(false)
	protected readonly engineError = signal(false)

	protected readonly readyStyles = computed(() =>
		this.styles().filter(style => style.status === 'ready' && style.tilesUrlTemplate),
	)

	protected readonly activeStyle = computed(() =>
		this.readyStyles().find(style => style.id === this.selectedStyleId()),
	)

	protected readonly styleName = (id: string): string => {
		const name = this.styles().find(style => style.id === id)?.name ?? id

		return id === this.defaultStyleId() ? `${name} (Default)` : name
	}

	#engine: MapEngine | undefined
	#initializing = ''
	#loadedId = ''
	#initialization = 0

	constructor() {
		afterRenderEffect(() => {
			const geometry = this.geometry()
			const style = this.activeStyle()

			if (this.#loadedId === geometry.id) {
				this.#engine?.setStyle(style)
			} else if (this.#initializing !== geometry.id) {
				const initialization = ++this.#initialization

				this.#initializing = geometry.id
				this.#loadedId = ''
				this.#engine?.dispose()
				this.#engine = undefined

				void import('./map-engine')
					.then(({ MapEngine }) => {
						if (this.#destroy.destroyed || initialization !== this.#initialization) return

						this.#engine = new MapEngine(this.canvas().nativeElement, geometry)
						this.#loadedId = geometry.id
						this.#engine.setStyle(this.activeStyle())
					})
					.catch(() => this.engineError.set(true))
			}
		})

		afterNextRender(() => {
			const document = this.#host.nativeElement.ownerDocument

			this.fullscreenAvailable.set(!!document.fullscreenEnabled)

			const update = (): void => this.fullscreen.set(document.fullscreenElement === this.#host.nativeElement)

			document.addEventListener('fullscreenchange', update)
			this.#destroy.onDestroy(() => document.removeEventListener('fullscreenchange', update))
		})

		this.#destroy.onDestroy(() => this.#engine?.dispose())
	}

	protected zoom(delta: number): void {
		this.#engine?.zoom(delta)
	}

	protected fit(): void {
		this.#engine?.fit()
	}

	protected toggleFullscreen(): void {
		const host = this.#host.nativeElement
		const operation = this.fullscreen() ? host.ownerDocument.exitFullscreen() : host.requestFullscreen()

		void operation.catch(() => this.fullscreenAvailable.set(false))
	}
}
