import { type BooleanInput } from '@angular/cdk/coercion'
import { CdkContextMenuTrigger } from '@angular/cdk/menu'
import { booleanAttribute, computed, Directive, effect, forwardRef, inject, input } from '@angular/core'
import { createMenuPosition, MENU_SIDE, type MenuAlign, type MenuSide } from '@spartan-ng/brain/core'
import { classes } from '@spartan-ng/helm/utils'
import { injectHlmContextMenuConfig } from './hlm-context-menu-token'

@Directive({
	selector: '[hlmContextMenuTrigger]',
	exportAs: 'hlmContextMenuTrigger',
	providers: [{ provide: MENU_SIDE, useExisting: forwardRef(() => HlmContextMenuTrigger) }],
	hostDirectives: [
		{
			directive: CdkContextMenuTrigger,
			inputs: [
				'cdkContextMenuTriggerFor: hlmContextMenuTrigger',
				'cdkContextMenuTriggerData: hlmContextMenuTriggerData',
				'cdkContextMenuDisabled: disabled',
			],
			outputs: ['cdkContextMenuOpened: hlmContextMenuOpened', 'cdkContextMenuClosed: hlmContextMenuClosed'],
		},
	],
	host: {
		'data-slot': 'context-menu-trigger',
		'[attr.data-disabled]': 'disabled() ? "" : null',
	},
})
export class HlmContextMenuTrigger {
	readonly #cdkTrigger = inject(CdkContextMenuTrigger, { host: true })
	readonly #config = injectHlmContextMenuConfig()

	public readonly disabled = input<boolean, BooleanInput>(this.#cdkTrigger.disabled, { transform: booleanAttribute })

	public readonly align = input<MenuAlign>(this.#config.align)
	public readonly side = input<MenuSide>(this.#config.side)

	readonly #menuPosition = computed(() => createMenuPosition(this.align(), this.side()))

	constructor() {
		// CDK sets transform-origin on the menu content (a shared hlm-dropdown-menu) from the resolved
		// position; the content reads it to animate from the anchored corner and to derive its data-side.
		// Cast tolerates @angular/cdk < 21.2 (we still support >=21.0), where the property is absent and
		// the assignment is a harmless no-op.
		;(this.#cdkTrigger as { transformOriginSelector?: string }).transformOriginSelector =
			'[data-slot="dropdown-menu"]'

		effect(() => {
			this.#cdkTrigger.menuPosition = this.#menuPosition()
		})

		classes(() => 'select-none')
	}
}
