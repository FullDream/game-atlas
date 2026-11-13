import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmSidebarGroup],hlm-sidebar-group',
	exportAs: 'hlmSidebarGroup',
	host: {
		'data-slot': 'sidebar-group',
		'data-sidebar': 'group',
	},
})
export class HlmSidebarGroup {
	constructor() {
		classes(() => 'min-w-0 p-2 relative flex w-full flex-col')
	}
}
