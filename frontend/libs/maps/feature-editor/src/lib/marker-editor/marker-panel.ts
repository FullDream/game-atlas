import { afterNextRender, DestroyRef, Directive, ElementRef, inject, input, output } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { BrnDialogRef } from '@spartan-ng/brain/dialog'

@Directive({ selector: '[mapsMarkerPanel]' })
export class MarkerPanel {
	readonly blockClose = input.required<() => boolean>()
	readonly confirmClose = input.required<() => Promise<boolean>>()

	readonly boundsChanged = output<DOMRect>()

	readonly #ref = inject(BrnDialogRef)
	readonly #element = inject(ElementRef<HTMLElement>)
	readonly #destroy = inject(DestroyRef)

	#permitted = false
	#confirming = false

	constructor() {
		this.#ref.closing$.pipe(takeUntilDestroyed()).subscribe(() => {
			if (this.#permitted || !this.blockClose()()) return

			this.#ref.reopen()
			this.#element.nativeElement.style.removeProperty('transform')
			if (this.#confirming) return

			this.#confirming = true
			void this.confirmClose()().then(allowed => {
				this.#confirming = false
				if (allowed && !this.#destroy.destroyed) {
					this.#permitted = true
					this.#ref.close()
				}
			})
		})
		afterNextRender(() => {
			const element = this.#element.nativeElement
			const measure = (): void => this.boundsChanged.emit(element.getBoundingClientRect())
			const observer = new ResizeObserver(measure)

			observer.observe(element)
			element.addEventListener('animationend', measure)
			window.addEventListener('resize', measure)
			this.#destroy.onDestroy(() => {
				observer.disconnect()
				element.removeEventListener('animationend', measure)
				window.removeEventListener('resize', measure)
			})
			measure()
		})
	}
}
