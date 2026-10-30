import { computed, Directive, ElementRef, inject, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: 'button[hlmAttachmentTrigger],a[hlmAttachmentTrigger]',
	exportAs: 'hlmAttachmentTrigger',
	host: {
		'data-slot': 'attachment-trigger',
		'[attr.type]': 'hostType()',
	},
})
export class HlmAttachmentTrigger {
	readonly #elementRef = inject(ElementRef<HTMLElement>)

	readonly type = input<'button' | 'submit' | 'reset' | null>('button')

	protected readonly hostType = computed(() =>
		this.#elementRef.nativeElement.localName === 'button' ? this.type() : null,
	)

	constructor() {
		classes(() => 'inset-0 absolute z-10 outline-none')
	}
}
