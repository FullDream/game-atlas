import { type BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: 'button[hlmSidebarMenuAction]',
	exportAs: 'hlmSidebarMenuAction',
	host: {
		'data-slot': 'sidebar-menu-action',
		'data-sidebar': 'menu-action',
	},
})
export class HlmSidebarMenuAction {
	public readonly showOnHover = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	constructor() {
		classes(() => [
			'text-sidebar-foreground ring-sidebar-ring hover:bg-sidebar-accent hover:text-sidebar-accent-foreground peer-hover/menu-button:text-sidebar-accent-foreground end-1 top-1.5 w-5 rounded-md p-0 peer-data-[size=default]/menu-button:top-2 peer-data-[size=lg]/menu-button:top-2.5 peer-data-[size=sm]/menu-button:top-1 [&>ng-icon]:text-[length:--spacing(4)] after:-inset-2 md:after:hidden absolute flex aspect-square items-center justify-center outline-hidden transition-transform group-data-[collapsible=icon]:hidden after:absolute focus-visible:ring-2 [&>ng-icon]:shrink-0',
			this.showOnHover() &&
				'peer-data-active/menu-button:text-sidebar-accent-foreground md:opacity-0 group-focus-within/menu-item:opacity-100 group-hover/menu-item:opacity-100 aria-expanded:opacity-100',
		])
	}
}
