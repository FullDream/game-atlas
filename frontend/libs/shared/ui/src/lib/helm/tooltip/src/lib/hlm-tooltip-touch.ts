import { InputModalityDetector } from '@angular/cdk/a11y'
import { DestroyRef, Directive, ElementRef, inject } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { BrnTooltip } from '@spartan-ng/brain/tooltip'
import { fromEvent } from 'rxjs'

@Directive({
	selector: '[hlmTooltipTouch]',
	host: {
		'(pointerdown)': 'start($event)',
		'(pointermove)': 'move($event)',
		'(pointerup)': 'cancel()',
		'(pointercancel)': 'cancel()',
		'(pointerleave)': 'leave($event)',
		'(contextmenu)': 'contextMenu($event)',
	},
})
export class HlmTooltipTouch {
	readonly #element = inject(ElementRef<HTMLElement>)
	readonly #tooltip = inject(BrnTooltip, { self: true })
	readonly #modality = inject(InputModalityDetector)
	readonly #destroy = inject(DestroyRef)
	#press: { x: number; y: number } | null = null
	#timer: ReturnType<typeof setTimeout> | undefined
	#held = false
	#suppressClick = false

	constructor() {
		fromEvent<FocusEvent>(this.#element.nativeElement, 'focus', { capture: true })
			.pipe(takeUntilDestroyed())
			.subscribe(event => {
				if (this.#modality.mostRecentModality === 'touch') event.stopImmediatePropagation()
			})

		fromEvent<MouseEvent>(this.#element.nativeElement, 'click', { capture: true })
			.pipe(takeUntilDestroyed())
			.subscribe(event => {
				const touchClick = event instanceof PointerEvent && event.pointerType === 'touch'

				if (!this.#suppressClick || (event.detail === 0 && !touchClick)) return

				event.preventDefault()
				event.stopImmediatePropagation()
				this.#suppressClick = false
			})

		this.#destroy.onDestroy(() => clearTimeout(this.#timer))
	}

	protected start(event: PointerEvent): void {
		this.#suppressClick = false

		if (event.pointerType !== 'touch') return

		this.cancel()

		if (!event.isPrimary) return

		this.#press = { x: event.clientX, y: event.clientY }
		this.#timer = setTimeout(() => {
			if (this.#tooltip.mutableTooltipDisabled() || !this.#tooltip.brnTooltip()) return

			this.#held = true
			this.#suppressClick = true
			this.#hover('pointerenter')
		}, 500)
	}

	protected move(event: PointerEvent): void {
		if (!this.#press || event.pointerType !== 'touch') return

		if (Math.hypot(event.clientX - this.#press.x, event.clientY - this.#press.y) > 10) this.cancel()
	}

	protected leave(event: PointerEvent): void {
		if (event.pointerType === 'touch') this.cancel()
	}

	protected cancel(): void {
		clearTimeout(this.#timer)
		this.#press = null

		if (this.#held) {
			this.#held = false
			this.#hover('pointerleave')
		}
	}

	protected contextMenu(event: MouseEvent): void {
		if (!this.#press && !this.#held) return

		event.preventDefault()
		event.stopPropagation()
	}

	#hover(type: 'pointerenter' | 'pointerleave'): void {
		// Brain exposes hover events, but no imperative show/hide API for a touch adapter.
		this.#element.nativeElement.dispatchEvent(new PointerEvent(type, { pointerType: 'mouse' }))
	}
}
