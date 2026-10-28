import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: 'ul[hlmSidebarMenuSub]',
	exportAs: 'hlmSidebarMenuSub',
	host: {
		'data-slot': 'sidebar-menu-sub',
		'data-sidebar': 'menu-sub',
	},
})
export class HlmSidebarMenuSub {
	constructor() {
		classes(
			() =>
				'mx-3.5 min-w-0 gap-1 border-sidebar-border px-2.5 py-0.5 flex translate-x-px flex-col border-s group-data-[collapsible=icon]:hidden rtl:-translate-x-px',
		)
	}
}
