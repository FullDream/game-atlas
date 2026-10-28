import { afterRenderEffect, Directive, ElementRef, inject } from '@angular/core'
import { BrnTooltip } from '@spartan-ng/brain/tooltip'

@Directive({ selector: '[hlmTooltip][uiTooltipOnOverflow]' })
export class TooltipOnOverflow {
	readonly #element = inject(ElementRef<HTMLElement>)
	readonly #tooltip = inject(BrnTooltip, { self: true })

	constructor() {
		afterRenderEffect(onCleanup => {
			const element = this.#element.nativeElement
			const disabled = this.#tooltip.tooltipDisabled() || !this.#tooltip.brnTooltip()
			const update = (): void => {
				this.#tooltip.mutableTooltipDisabled.set(disabled || element.scrollWidth <= element.clientWidth)
			}

			update()

			const observer = new ResizeObserver(update)

			observer.observe(element)
			onCleanup(() => observer.disconnect())
		})
	}
}
