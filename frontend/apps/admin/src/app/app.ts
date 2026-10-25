import { Component, inject } from '@angular/core'
import { toSignal } from '@angular/core/rxjs-interop'
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { ThemeSwitcher } from '@game-atlas/shared/ui'
import { matSportsEsportsRound, matPhotoLibraryRound, matMapRound } from '@ng-icons/material-symbols/round'
import {
	HlmSidebar,
	HlmSidebarWrapper,
	HlmSidebarHeader,
	HlmSidebarContent,
	HlmSidebarGroup,
	HlmSidebarGroupLabel,
	HlmSidebarMenu,
	HlmSidebarMenuItem,
	HlmSidebarMenuButton,
	HlmSidebarTrigger,
	HlmSidebarInset,
	HlmSidebarService,
	provideHlmSidebarConfig,
} from '@spartan-ng/helm/sidebar'
import { filter, map } from 'rxjs'
import { ThemeSettings } from './theme-settings'

@Component({
	selector: 'app-root',
	imports: [
		RouterLink,
		RouterLinkActive,
		RouterOutlet,
		NgIcon,
		ThemeSwitcher,
		HlmSidebar,
		HlmSidebarWrapper,
		HlmSidebarHeader,
		HlmSidebarContent,
		HlmSidebarGroup,
		HlmSidebarGroupLabel,
		HlmSidebarMenu,
		HlmSidebarMenuItem,
		HlmSidebarMenuButton,
		HlmSidebarTrigger,
		HlmSidebarInset,
	],
	providers: [
		provideIcons({ matSportsEsportsRound, matPhotoLibraryRound, matMapRound }),
		provideHlmSidebarConfig({ closeMobileSidebarOnMenuButtonClick: true }),
		HlmSidebarService,
	],
	templateUrl: './app.html',
	host: { class: 'block min-h-dvh' },
})
export class App {
	protected readonly sidebar = inject(HlmSidebarService)
	protected readonly themeSettings = inject(ThemeSettings)
	readonly #router = inject(Router)

	protected readonly navigation = [
		{ label: 'Games', path: '/games', icon: 'matSportsEsportsRound' },
		{ label: 'Media', path: '/media', icon: 'matPhotoLibraryRound' },
	]

	protected readonly heading = toSignal(
		this.#router.events.pipe(
			filter(event => event instanceof NavigationEnd),
			map(() => String(this.#router.routerState.snapshot.root.firstChild?.data['section'] ?? 'Games')),
		),
		{ initialValue: 'Games' },
	)
}
