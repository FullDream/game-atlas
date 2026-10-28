import { Component, inject } from '@angular/core'
import { RouterOutlet } from '@angular/router'
import { HlmSidebarService, provideHlmSidebarConfig } from '@spartan-ng/helm/sidebar'
import { HlmToaster } from '@spartan-ng/helm/sonner'
import { ThemeSettings } from './theme-settings'

@Component({
	selector: 'app-root',
	imports: [RouterOutlet, HlmToaster],
	templateUrl: './app.html',
	providers: [provideHlmSidebarConfig({ closeMobileSidebarOnMenuButtonClick: true }), HlmSidebarService],
	host: { class: 'block h-dvh overflow-hidden' },
})
export class App {
	protected readonly themeSettings = inject(ThemeSettings)
}
