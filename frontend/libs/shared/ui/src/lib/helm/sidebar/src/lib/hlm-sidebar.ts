import { NgTemplateOutlet } from '@angular/common'
import { Component, computed, effect, inject, input } from '@angular/core'
import { HlmSheetImports } from '@spartan-ng/helm/sheet'
import { classes, hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'
import { HlmSidebarService, type SidebarVariant } from './hlm-sidebar.service'
import { injectHlmSidebarConfig } from './hlm-sidebar.token'

@Component({
	selector: 'hlm-sidebar',
	exportAs: 'hlmSidebar',
	imports: [NgTemplateOutlet, HlmSheetImports],
	templateUrl: './hlm-sidebar.html',
	host: {
		'[attr.data-slot]': 'dataSlot()',
		'[attr.data-state]': 'dataState()',
		'[attr.data-collapsible]': 'dataCollapsible()',
		'[attr.data-variant]': 'dataVariant()',
		'[attr.data-side]': 'dataSide()',
	},
})
export class HlmSidebar {
	protected readonly sidebarService = inject(HlmSidebarService)
	readonly #config = injectHlmSidebarConfig()
	public readonly sidebarWidthMobile = input<string>(this.#config.sidebarWidthMobile)

	public readonly side = input<'left' | 'right'>('left')
	public readonly variant = input<SidebarVariant>(this.sidebarService.variant())
	public readonly collapsible = input<'offcanvas' | 'icon' | 'none'>('offcanvas')

	public readonly srOnlySheetTitle = input<string>('Sidebar')
	public readonly srOnlySheetDescription = input<string>('Displays the mobile sidebar.')

	protected readonly sidebarGapComputedClass = computed(() =>
		hlm(
			'relative w-(--sidebar-width) bg-transparent transition-[width] duration-200 ease-linear',
			'group-data-[collapsible=offcanvas]:w-0',
			'group-data-[side=right]:rotate-180',
			this.variant() === 'floating' || this.variant() === 'inset'
				? 'group-data-[collapsible=icon]:w-[calc(var(--sidebar-width-icon)+(--spacing(4)))]'
				: 'group-data-[collapsible=icon]:w-(--sidebar-width-icon)',
		),
	)

	public readonly sidebarContainerClass = input<ClassValue>('')

	protected readonly sidebarContainerComputedClass = computed(() =>
		hlm(
			'inset-y-0 data-[side=left]:left-0 data-[side=right]:right-0 md:flex fixed z-10 hidden h-svh w-(--sidebar-width) transition-[left,right,width] duration-200 ease-linear data-[side=left]:group-data-[collapsible=offcanvas]:left-[calc(var(--sidebar-width)*-1)] data-[side=right]:group-data-[collapsible=offcanvas]:right-[calc(var(--sidebar-width)*-1)]',
			this.variant() === 'floating' || this.variant() === 'inset'
				? 'p-2 group-data-[collapsible=icon]:w-[calc(var(--sidebar-width-icon)+(--spacing(4))+2px)]'
				: 'group-data-[collapsible=icon]:w-(--sidebar-width-icon) group-data-[side=left]:border-r group-data-[side=right]:border-l',
			this.sidebarContainerClass(),
		),
	)

	protected readonly dataSlot = computed(() => {
		return !this.sidebarService.isMobile() ? 'sidebar' : undefined
	})

	readonly #collapsibleAndNonMobile = computed(() => {
		return this.collapsible() !== 'none' && !this.sidebarService.isMobile()
	})

	protected readonly dataState = computed(() => {
		return this.#collapsibleAndNonMobile() ? this.sidebarService.state() : undefined
	})

	protected readonly dataCollapsible = computed(() => {
		if (this.#collapsibleAndNonMobile()) {
			return this.sidebarService.state() === 'collapsed' ? this.collapsible() : ''
		}

		return undefined
	})

	protected readonly dataVariant = computed(() => {
		return this.#collapsibleAndNonMobile() ? this.variant() : undefined
	})

	protected readonly dataSide = computed(() => {
		return this.#collapsibleAndNonMobile() ? this.side() : undefined
	})

	constructor() {
		// Sync variant input with service
		effect(() => {
			this.sidebarService.setVariant(this.variant())
		})

		classes(() => {
			if (this.collapsible() === 'none') {
				return hlm('bg-sidebar text-sidebar-foreground flex h-svh w-(--sidebar-width) flex-col')
			} else if (this.sidebarService.isMobile()) {
				return ''
			} else {
				return hlm('group peer text-sidebar-foreground md:block hidden')
			}
		})
	}
}
