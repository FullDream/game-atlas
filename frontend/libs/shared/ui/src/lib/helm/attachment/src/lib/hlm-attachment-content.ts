import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmAttachmentContent],hlm-attachment-content',
	exportAs: 'hlmAttachmentContent',
	host: { 'data-slot': 'attachment-content' },
})
export class HlmAttachmentContent {
	constructor() {
		classes(() => 'min-w-0 leading-snug group-data-[orientation=vertical]/attachment:px-1 max-w-full flex-1')
	}
}
