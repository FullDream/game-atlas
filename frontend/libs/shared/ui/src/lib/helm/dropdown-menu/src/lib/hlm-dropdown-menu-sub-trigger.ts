import { CdkMenuTrigger } from '@angular/cdk/menu'
import { computed, Directive, effect, forwardRef, inject, input } from '@angular/core'
import {
	createMenuPosition,
	MENU_SIDE,
	type MenuAlign,
	type MenuSide,
	updateMenuPosition,
} from '@spartan-ng/brain/core'
import { classes } from '@spartan-ng/helm/utils'
import { injectHlmDropdownMenuConfig } from './hlm-dropdown-menu-token'

@Directive({
	selector: '[hlmDropdownMenuSubTrigger]',
	exportAs: 'hlmDropdownMenuSubTrigger',
	providers: [{ provide: MENU_SIDE, useExisting: forwardRef(() => HlmDropdownMenuSubTrigger) }],
	hostDirectives: [
		{
			directive: CdkMenuTrigger,
			inputs: ['cdkMenuTriggerFor: hlmDropdownMenuSubTrigger', 'cdkMenuTriggerData: hlmDropdownMenuTriggerData'],
			outputs: ['cdkMenuOpened: hlmDropdownMenuSubOpened', 'cdkMenuClosed: hlmDropdownMenuSubClosed'],
		},
	],
	host: { 'data-slot': 'dropdown-menu-sub-trigger' },
})
export class HlmDropdownMenuSubTrigger {
	readonly #cdkTrigger = inject(CdkMenuTrigger, { host: true })
	readonly #config = injectHlmDropdownMenuConfig()

	public readonly align = input<MenuAlign>(this.#config.align)
	public readonly side = input<MenuSide>(this.#config.side)

	readonly #menuPosition = computed(() => createMenuPosition(this.align(), this.side()))

	constructor() {
		// CDK sets transform-origin on the submenu content from the resolved position; the content reads it
		// to animate from the anchored corner and to derive its data-side. Cast tolerates @angular/cdk < 21.2
		// (we still support >=21.0), where the property is absent and the assignment is a harmless no-op.
		;(this.#cdkTrigger as { transformOriginSelector?: string }).transformOriginSelector =
			'[data-slot="dropdown-menu-sub"]'

		effect(() => {
			updateMenuPosition(this.#cdkTrigger, this.#menuPosition())
		})

		classes(() => 'aria-expanded:bg-accent aria-expanded:text-accent-foreground')
	}
}
