import { type BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, computed, Directive, effect, inject, input } from '@angular/core'
import { BrnTooltip, BrnTooltipPosition, provideBrnTooltipDefaultOptions } from '@spartan-ng/brain/tooltip'
import {
	DEFAULT_TOOLTIP_CONTENT_CLASSES,
	DEFAULT_TOOLTIP_SVG_CLASS,
	tooltipPositionVariants,
	HlmTooltipTouch,
} from '@spartan-ng/helm/tooltip'
import { classes, hlm } from '@spartan-ng/helm/utils'
import { cva } from 'class-variance-authority'
import { HlmSidebarService } from './hlm-sidebar.service'
import { injectHlmSidebarConfig } from './hlm-sidebar.token'

const sidebarMenuButtonVariants = cva(
	'peer/menu-button group/menu-button gap-2 rounded-md p-2 text-sm ring-sidebar-ring group-has-data-[sidebar=menu-action]/menu-item:pe-8 group-data-[collapsible=icon]:size-10! group-data-[collapsible=icon]:p-2.5! hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground data-open:hover:bg-sidebar-accent data-open:hover:text-sidebar-accent-foreground data-active:bg-sidebar-accent data-active:font-medium data-active:text-sidebar-accent-foreground [&_ng-icon]:text-[length:--spacing(5)] flex w-full items-center overflow-hidden text-start outline-hidden transition-[width,height,padding] focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 [&_ng-icon]:shrink-0 [&>span:last-child]:truncate',
	{
		variants: {
			variant: {
				default: 'hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
				outline:
					'bg-background hover:bg-sidebar-accent hover:text-sidebar-accent-foreground shadow-[0_0_0_1px_var(--sidebar-border)] hover:shadow-[0_0_0_1px_var(--sidebar-accent)]',
			},
			size: {
				default: 'h-10 gap-3 px-3 text-sm',
				sm: 'h-7 text-xs',
				lg: 'h-12 text-sm group-data-[collapsible=icon]:p-0!',
			},
		},
		defaultVariants: {
			variant: 'default',
			size: 'default',
		},
	},
)

@Directive({
	selector: 'button[hlmSidebarMenuButton], a[hlmSidebarMenuButton]',
	exportAs: 'hlmSidebarMenuButton',
	providers: [
		provideBrnTooltipDefaultOptions({
			showDelay: 150,
			hideDelay: 0,
			tooltipContentClasses: DEFAULT_TOOLTIP_CONTENT_CLASSES,
			svgClasses: DEFAULT_TOOLTIP_SVG_CLASS,
			arrowClasses: (position: BrnTooltipPosition) => hlm(tooltipPositionVariants({ position })),
			position: 'right',
		}),
	],
	hostDirectives: [
		{
			directive: BrnTooltip,
			inputs: ['brnTooltip: tooltip'],
		},
		HlmTooltipTouch,
	],
	host: {
		'data-slot': 'sidebar-menu-button',
		'data-sidebar': 'menu-button',
		'[attr.data-size]': 'size()',
		'[attr.data-active]': 'isActive()',
		'(click)': 'onClick()',
	},
})
export class HlmSidebarMenuButton {
	readonly #config = injectHlmSidebarConfig()
	readonly #sidebarService = inject(HlmSidebarService)
	readonly #brnTooltip = inject(BrnTooltip)

	public readonly variant = input<'default' | 'outline'>('default')
	public readonly size = input<'default' | 'sm' | 'lg'>('default')
	public readonly isActive = input<boolean, BooleanInput>(false, { transform: booleanAttribute })
	public readonly closeMobileSidebarOnClick = input<boolean, BooleanInput>(
		this.#config.closeMobileSidebarOnMenuButtonClick,
		{ transform: booleanAttribute },
	)

	protected readonly isTooltipHidden = computed(
		() => this.#sidebarService.state() !== 'collapsed' || this.#sidebarService.isMobile(),
	)

	constructor() {
		classes(() => sidebarMenuButtonVariants({ variant: this.variant(), size: this.size() }))
		effect(() => this.#brnTooltip.mutableTooltipDisabled.set(this.isTooltipHidden()))
	}

	protected onClick(): void {
		if (this.closeMobileSidebarOnClick()) {
			this.#sidebarService.setOpenMobile(false)
		}
	}
}
