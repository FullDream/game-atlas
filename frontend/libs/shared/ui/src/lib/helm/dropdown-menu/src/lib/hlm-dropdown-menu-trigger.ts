import { CdkMenuTrigger } from '@angular/cdk/menu'
import { computed, Directive, effect, forwardRef, inject, input } from '@angular/core'
import {
	createMenuPosition,
	MENU_SIDE,
	type MenuAlign,
	type MenuSide,
	updateMenuPosition,
} from '@spartan-ng/brain/core'
import { injectHlmDropdownMenuConfig } from './hlm-dropdown-menu-token'

@Directive({
	selector: '[hlmDropdownMenuTrigger]',
	exportAs: 'hlmDropdownMenuTrigger',
	providers: [{ provide: MENU_SIDE, useExisting: forwardRef(() => HlmDropdownMenuTrigger) }],
	hostDirectives: [
		{
			directive: CdkMenuTrigger,
			inputs: ['cdkMenuTriggerFor: hlmDropdownMenuTrigger', 'cdkMenuTriggerData: hlmDropdownMenuTriggerData'],
			outputs: ['cdkMenuOpened: hlmDropdownMenuOpened', 'cdkMenuClosed: hlmDropdownMenuClosed'],
		},
	],
	host: { 'data-slot': 'dropdown-menu-trigger' },
})
export class HlmDropdownMenuTrigger {
	readonly #cdkTrigger = inject(CdkMenuTrigger, { host: true })
	readonly #config = injectHlmDropdownMenuConfig()

	public readonly align = input<MenuAlign>(this.#config.align)
	public readonly side = input<MenuSide>(this.#config.side)

	readonly #menuPosition = computed(() => createMenuPosition(this.align(), this.side()))

	constructor() {
		// CDK sets transform-origin on the menu content from the resolved position; the content reads it to
		// animate from the anchored corner and to derive its data-side. Cast tolerates @angular/cdk < 21.2
		// (we still support >=21.0), where the property is absent and the assignment is a harmless no-op.
		;(this.#cdkTrigger as { transformOriginSelector?: string }).transformOriginSelector =
			'[data-slot="dropdown-menu"]'

		effect(() => {
			updateMenuPosition(this.#cdkTrigger, this.#menuPosition())
		})
	}
}
