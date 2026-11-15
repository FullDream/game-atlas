import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmCardHeader],hlm-card-header',
	exportAs: 'hlmCardHeader',
	host: { 'data-slot': 'card-header' },
})
export class HlmCardHeader {
	constructor() {
		classes(
			() =>
				'gap-2 rounded-t-xl group/card-header @container/card-header grid auto-rows-min items-start px-(--card-spacing) has-data-[slot=card-action]:grid-cols-[1fr_auto] has-data-[slot=card-description]:grid-rows-[auto_auto] [.border-b]:pb-(--card-spacing)',
		)
	}
}
