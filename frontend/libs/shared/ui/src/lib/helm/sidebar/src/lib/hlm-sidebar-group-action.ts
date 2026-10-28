import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: 'button[hlmSidebarGroupAction]',
	exportAs: 'hlmSidebarGroupAction',
	host: {
		'data-slot': 'sidebar-group-action',
		'data-sidebar': 'group-action',
	},
})
export class HlmSidebarGroupAction {
	constructor() {
		classes(
			() =>
				'end-3 top-3.5 w-5 rounded-md p-0 text-sidebar-foreground ring-sidebar-ring after:-inset-2 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground md:after:hidden [&>ng-icon]:text-[length:--spacing(4)] absolute flex aspect-square items-center justify-center outline-hidden transition-transform group-data-[collapsible=icon]:hidden after:absolute focus-visible:ring-2 [&>ng-icon]:shrink-0',
		)
	}
}
