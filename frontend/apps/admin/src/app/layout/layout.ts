import { CdkDropListGroup } from '@angular/cdk/drag-drop'
import { CdkPortalOutlet } from '@angular/cdk/portal'
import { Component, inject } from '@angular/core'
import { toSignal } from '@angular/core/rxjs-interop'
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router'
import { PageHeaderOutlet, PageLoading, PageProgress, ThemeSwitcher, WorkspaceBreadcrumbs } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound, matPhotoLibraryRound, matSportsEsportsRound } from '@ng-icons/material-symbols/round'
import {
	HlmSidebar,
	HlmSidebarContent,
	HlmSidebarFooter,
	HlmSidebarGroup,
	HlmSidebarGroupLabel,
	HlmSidebarHeader,
	HlmSidebarInset,
	HlmSidebarMenu,
	HlmSidebarMenuButton,
	HlmSidebarMenuItem,
	HlmSidebarService,
	HlmSidebarTrigger,
	HlmSidebarWrapper,
} from '@spartan-ng/helm/sidebar'

import { filter, map } from 'rxjs'
import { ThemeSettings } from '../theme-settings'

@Component({
	selector: 'app-layout',
	imports: [
		CdkDropListGroup,
		RouterLink,
		RouterLinkActive,
		RouterOutlet,
		NgIcon,
		ThemeSwitcher,
		WorkspaceBreadcrumbs,
		PageHeaderOutlet,
		CdkPortalOutlet,
		PageProgress,
		HlmSidebar,
		HlmSidebarWrapper,
		HlmSidebarHeader,
		HlmSidebarContent,
		HlmSidebarFooter,
		HlmSidebarGroup,
		HlmSidebarGroupLabel,
		HlmSidebarMenu,
		HlmSidebarMenuItem,
		HlmSidebarMenuButton,
		HlmSidebarTrigger,
		HlmSidebarInset,
	],
	templateUrl: './layout.html',
	providers: [PageLoading, provideIcons({ matSportsEsportsRound, matPhotoLibraryRound, matMapRound })],
	host: { class: 'block h-dvh overflow-hidden' },
})
export class Layout {
	protected readonly sidebar = inject(HlmSidebarService)
	protected readonly themeSettings = inject(ThemeSettings)
	readonly #router = inject(Router)
	readonly #activatedRoute = inject(ActivatedRoute)

	protected readonly navigation = [
		{ label: 'Games', path: '/games', icon: 'matSportsEsportsRound' },
		{ label: 'Media', path: '/media', icon: 'matPhotoLibraryRound' },
	]

	protected readonly heading = toSignal(
		this.#router.events.pipe(
			filter(event => event instanceof NavigationEnd),
			map(() => String(this.#activatedRoute.snapshot.firstChild?.data['section'] ?? 'Games')),
		),
		{ initialValue: String(this.#activatedRoute.snapshot.firstChild?.data['section'] ?? 'Games') },
	)
}
