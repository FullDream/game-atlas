import { injectMutation } from '@tanstack/angular-query'
import { AuthQueryOptions, injectLoginRedirect } from '@game-atlas/auth/data-access'
import { CdkDropListGroup } from '@angular/cdk/drag-drop'
import { CdkPortalOutlet } from '@angular/cdk/portal'
import { Component, inject } from '@angular/core'
import { toSignal } from '@angular/core/rxjs-interop'
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router'
import { PageHeaderOutlet, PageLoading, PageProgress, ThemeSwitcher, WorkspaceBreadcrumbs } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matLogoutRound,
	matMapRound,
	matPhotoLibraryRound,
	matSportsEsportsRound,
} from '@ng-icons/material-symbols/round'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog'
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
import { HlmSpinner } from '@spartan-ng/helm/spinner'

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
		HlmAlertDialogImports,
		HlmSpinner,
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
	providers: [
		PageLoading,
		provideIcons({ matLogoutRound, matMapRound, matPhotoLibraryRound, matSportsEsportsRound }),
	],
	host: { class: 'block h-dvh overflow-hidden' },
})
export class Layout {
	protected readonly sidebar = inject(HlmSidebarService)
	protected readonly themeSettings = inject(ThemeSettings)
	readonly #auth = inject(AuthQueryOptions)
	readonly #redirect = injectLoginRedirect()
	readonly #router = inject(Router)
	readonly #activatedRoute = inject(ActivatedRoute)

	protected readonly navigation = [
		{ label: 'Games', path: '/games', icon: 'matSportsEsportsRound' },
		{ label: 'Media', path: '/media', icon: 'matPhotoLibraryRound' },
	]

	protected readonly logout = injectMutation(() => this.#auth.logout())

	protected readonly heading = toSignal(
		this.#router.events.pipe(
			filter(event => event instanceof NavigationEnd),
			map(() => String(this.#activatedRoute.snapshot.firstChild?.data['section'] ?? 'Games')),
		),
		{ initialValue: String(this.#activatedRoute.snapshot.firstChild?.data['section'] ?? 'Games') },
	)

	protected async signOut(close: () => void): Promise<void> {
		if (this.logout.isPending()) return

		try {
			await this.logout.mutateAsync()
			this.#redirect('/games')
			close()
		} catch {
			toast.error('Unable to sign out right now. Please try again.')
		}
	}
}
