import { Directive, computed, effect, input, untracked } from '@angular/core'
import { injectCustomClassSettable } from '@spartan-ng/brain/core'
import { BrnDrawerOverlay } from '@spartan-ng/brain/drawer'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'

@Directive({
	selector: '[hlmDrawerOverlay],hlm-drawer-overlay',
	exportAs: 'hlmDrawerOverlay',
	hostDirectives: [BrnDrawerOverlay],
})
export class HlmDrawerOverlay {
	readonly userClass = input<ClassValue>('', { alias: 'class' })

	readonly #classSettable = injectCustomClassSettable({ optional: true, host: true })

	protected readonly computedClass = computed(() =>
		hlm(
			'data-open:animate-in data-closed:animate-out data-closed:fade-out-0 data-open:fade-in-0 bg-black/10 transition-opacity duration-150 data-ending-style:opacity-0 data-starting-style:opacity-0',
			this.userClass(),
		),
	)

	constructor() {
		effect(() => {
			const classValue = this.computedClass()

			untracked(() => this.#classSettable?.setClassToCustomElement(classValue))
		})
	}
}
