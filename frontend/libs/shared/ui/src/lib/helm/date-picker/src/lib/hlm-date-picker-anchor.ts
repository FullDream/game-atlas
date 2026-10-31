import { Directive, effect, ElementRef, inject, input, linkedSignal } from '@angular/core'
import { BrnOverlay } from '@spartan-ng/brain/overlay'
import { BrnPopover } from '@spartan-ng/brain/popover'

@Directive({ selector: '[hlmDatePickerAnchor]', exportAs: 'hlmDatePickerAnchor' })
export class HlmDatePickerAnchor {
	readonly #host = inject(ElementRef, { host: true })
	readonly #brnOverlay = inject(BrnOverlay, { optional: true })

	readonly hlmDatePickerAnchorForInput = input<BrnPopover | undefined>(undefined, {
		alias: 'hlmDatePickerAnchorFor',
	})

	readonly hlmDatePickerAnchorFor = linkedSignal(this.hlmDatePickerAnchorForInput)

	constructor() {
		effect(() => {
			this.hlmDatePickerAnchorFor()?.setOrigin(this.#host.nativeElement)
		})

		this.#brnOverlay?.setOrigin(this.#host.nativeElement)
	}
}
