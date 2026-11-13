import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmAttachmentGroup],hlm-attachment-group',
	exportAs: 'hlmAttachmentGroup',
	host: { 'data-slot': 'attachment-group' },
})
export class HlmAttachmentGroup {
	constructor() {
		classes(
			() =>
				'scroll-fade-x no-scrollbar min-w-0 scroll-px-1 gap-3 py-1 flex snap-x snap-mandatory overflow-x-auto overscroll-x-contain *:data-[slot=attachment]:flex-none *:data-[slot=attachment]:snap-start',
		)
	}
}
