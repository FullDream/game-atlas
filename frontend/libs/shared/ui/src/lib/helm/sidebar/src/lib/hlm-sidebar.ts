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
	host: {
		'[attr.data-slot]': 'dataSlot()',
		'[attr.data-state]': 'dataState()',
		'[attr.data-collapsible]': 'dataCollapsible()',
		'[attr.data-variant]': 'dataVariant()',
		'[attr.data-side]': 'dataSide()',
	},
	template: `
		<ng-template #contentContainer>
			<ng-content />
		</ng-template>

		@if (collapsible() === 'none') {
			<ng-container *ngTemplateOutlet="contentContainer"></ng-container>
		} @else if (sidebarService.isMobile()) {
			<hlm-sheet
				[side]="side()"
				[state]="sidebarService.openMobile() ? 'open' : 'closed'"
				(stateChanged)="sidebarService.setOpenMobile($event === 'open')"
			>
				<hlm-sheet-content
					*hlmSheetPortal="let ctx"
					data-slot="sidebar"
					data-sidebar="sidebar"
					data-mobile="true"
					class="bg-sidebar p-0 text-sidebar-foreground h-svh w-(--sidebar-width) [&>button]:hidden"
					[style.--sidebar-width]="sidebarWidthMobile()"
				>
					<hlm-sheet-header class="sr-only">
						<h2 hlmSheetTitle>{{ srOnlySheetTitle() }}</h2>
						<p hlmSheetDescription>{{ srOnlySheetDescription() }}</p>
					</hlm-sheet-header>
					<div class="flex h-full w-full flex-col">
						<ng-container *ngTemplateOutlet="contentContainer" />
					</div>
				</hlm-sheet-content>
			</hlm-sheet>
		} @else {
			<!-- Sidebar gap on desktop -->
			<div data-slot="sidebar-gap" [class]="sidebarGapComputedClass()"></div>
			<div data-slot="sidebar-container" [attr.data-side]="dataSide()" [class]="sidebarContainerComputedClass()">
				<div
					data-sidebar="sidebar"
					data-slot="sidebar-inner"
					class="bg-sidebar group-data-[variant=floating]:rounded-lg group-data-[variant=floating]:shadow-sm group-data-[variant=floating]:ring-sidebar-border flex size-full flex-col group-data-[variant=floating]:ring-1"
				>
					<ng-container *ngTemplateOutlet="contentContainer" />
				</div>
			</div>
		}
	`,
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
