import { type BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Directive, inject, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { HlmSidebarService } from './hlm-sidebar.service'
import { injectHlmSidebarConfig } from './hlm-sidebar.token'

@Directive({
	selector: 'a[hlmSidebarMenuSubButton], button[hlmSidebarMenuSubButton]',
	exportAs: 'hlmSidebarMenuSubButton',
	host: {
		'data-slot': 'sidebar-menu-sub-button',
		'data-sidebar': 'menu-sub-button',
		'[attr.data-active]': 'isActive()',
		'[attr.data-size]': 'size()',
		'(click)': 'onClick()',
	},
})
export class HlmSidebarMenuSubButton {
	readonly #sidebarService = inject(HlmSidebarService)
	readonly #config = injectHlmSidebarConfig()

	public readonly closeMobileSidebarOnClick = input<boolean, BooleanInput>(
		this.#config.closeMobileSidebarOnMenuButtonClick,
		{ transform: booleanAttribute },
	)

	public readonly size = input<'sm' | 'md'>('md')
	public readonly isActive = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	constructor() {
		classes(
			() =>
				'h-7 min-w-0 gap-2 rounded-md px-2 text-sidebar-foreground ring-sidebar-ring hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground data-[size=md]:text-sm data-[size=sm]:text-xs data-active:bg-sidebar-accent data-active:text-sidebar-accent-foreground [&>ng-icon]:text-[length:--spacing(4)] [&>svg]:text-sidebar-accent-foreground flex -translate-x-px items-center overflow-hidden outline-hidden group-data-[collapsible=icon]:hidden focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 [&>ng-icon]:shrink-0 [&>span:last-child]:truncate',
		)
	}

	protected onClick(): void {
		if (this.closeMobileSidebarOnClick()) {
			this.#sidebarService.setOpenMobile(false)
		}
	}
}
