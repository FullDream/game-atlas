import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmAttachmentTitle],hlm-attachment-title',
	exportAs: 'hlmAttachmentTitle',
	host: { 'data-slot': 'attachment-title' },
})
export class HlmAttachmentTitle {
	constructor() {
		classes(
			() =>
				'text-sm font-medium group-data-[state=processing]/attachment:shimmer group-data-[state=uploading]/attachment:shimmer min-w-0 block max-w-full truncate',
		)
	}
}
