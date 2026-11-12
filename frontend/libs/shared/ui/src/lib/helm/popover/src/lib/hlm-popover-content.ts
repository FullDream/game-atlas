import { Directive, ElementRef, Renderer2, effect, inject, signal } from '@angular/core'
import { injectExposesStateProvider } from '@spartan-ng/brain/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmPopoverContent],hlm-popover-content',
	exportAs: 'hlmPopoverContent',
	host: { 'data-slot': 'popover-content' },
})
export class HlmPopoverContent {
	readonly #stateProvider = injectExposesStateProvider({ host: true })
	state = this.#stateProvider.state ?? signal('closed')
	readonly #renderer = inject(Renderer2)
	readonly #element = inject(ElementRef)

	constructor() {
		effect(() => {
			this.#renderer.setAttribute(this.#element.nativeElement, 'data-state', this.state())
		})

		classes(
			() =>
				'bg-popover text-popover-foreground data-open:animate-in data-closed:animate-out data-closed:fade-out-0 data-open:fade-in-0 data-closed:zoom-out-95 data-open:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 ring-foreground/5 gap-4 rounded-2xl p-4 text-sm shadow-2xl w-72 relative flex flex-col ring-1 duration-100 outline-none',
		)
	}
}
