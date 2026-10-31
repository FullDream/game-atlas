import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmPopoverHeader],hlm-popover-header',
	exportAs: 'hlmPopoverHeader',
	host: { 'data-slot': 'popover-header' },
})
export class HlmPopoverHeader {
	constructor() {
		classes(() => 'gap-1 text-sm flex flex-col')
	}
}
