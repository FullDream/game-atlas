import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmSidebarContent],hlm-sidebar-content',
	exportAs: 'hlmSidebarContent',
	host: {
		'data-slot': 'sidebar-content',
		'data-sidebar': 'content',
	},
})
export class HlmSidebarContent {
	constructor() {
		classes(
			() =>
				'no-scrollbar gap-2 min-h-0 flex flex-1 flex-col overflow-auto [--radius:var(--radius-xl)] group-data-[collapsible=icon]:overflow-hidden',
		)
	}
}
