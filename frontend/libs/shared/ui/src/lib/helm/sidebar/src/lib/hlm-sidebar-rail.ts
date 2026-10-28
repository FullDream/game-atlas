import { Directive, inject, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { HlmSidebarService } from './hlm-sidebar.service'

@Directive({
	selector: 'button[hlmSidebarRail]',
	exportAs: 'hlmSidebarRail',
	host: {
		'data-sidebar': 'rail',
		'data-slot': 'sidebar-rail',
		'[attr.aria-label]': 'ariaLabel()',
		tabindex: '-1',
		'(click)': 'onClick()',
	},
})
export class HlmSidebarRail {
	readonly #sidebarService = inject(HlmSidebarService)

	public readonly ariaLabel = input<string>('Toggle Sidebar', { alias: 'aria-label' })

	constructor() {
		classes(() => [
			'inset-y-0 w-4 group-data-[side=left]:-right-4 group-data-[side=right]:left-0 after:inset-y-0 hover:after:bg-sidebar-border sm:flex absolute z-20 hidden transition-all ease-linear after:absolute after:start-1/2 after:w-[2px] ltr:-translate-x-1/2 rtl:-translate-x-1/2',
			'in-data-[side=left]:cursor-w-resize in-data-[side=right]:cursor-e-resize',
			'[[data-side=left][data-state=collapsed]_&]:cursor-e-resize [[data-side=right][data-state=collapsed]_&]:cursor-w-resize',
			'group-data-[collapsible=offcanvas]:translate-x-0 hover:group-data-[collapsible=offcanvas]:bg-sidebar group-data-[collapsible=offcanvas]:after:left-full',
			'[[data-side=left][data-collapsible=offcanvas]_&]:-right-2',
			'[[data-side=right][data-collapsible=offcanvas]_&]:-left-2',
		])
	}

	protected onClick(): void {
		this.#sidebarService.toggleSidebar()
	}
}
