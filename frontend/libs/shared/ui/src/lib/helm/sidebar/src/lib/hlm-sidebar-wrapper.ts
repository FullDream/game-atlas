import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { injectHlmSidebarConfig } from './hlm-sidebar.token'

@Directive({
	selector: '[hlmSidebarWrapper],hlm-sidebar-wrapper',
	exportAs: 'hlmSidebarWrapper',
	host: {
		'data-slot': 'sidebar-wrapper',
		'[style.--sidebar-width]': 'sidebarWidth()',
		'[style.--sidebar-width-icon]': 'sidebarWidthIcon()',
	},
})
export class HlmSidebarWrapper {
	readonly #config = injectHlmSidebarConfig()

	public readonly sidebarWidth = input<string>(this.#config.sidebarWidth)
	public readonly sidebarWidthIcon = input<string>(this.#config.sidebarWidthIcon)

	constructor() {
		classes(() => 'group/sidebar-wrapper flex min-h-svh w-full has-data-[variant=inset]:bg-sidebar')
	}
}
