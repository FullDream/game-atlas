import { Component, inject } from '@angular/core'
import { RouterLink, RouterOutlet } from '@angular/router'
import { ThemeSwitcher } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound } from '@ng-icons/material-symbols/round'
import { ThemeSettings } from './theme-settings'

@Component({
	selector: 'app-root',
	imports: [RouterLink, RouterOutlet, NgIcon, ThemeSwitcher],
	templateUrl: './app.html',
	providers: [provideIcons({ matMapRound })],
	host: { class: 'flex min-h-dvh flex-col bg-background text-foreground' },
})
export class App {
	protected readonly themeSettings = inject(ThemeSettings)
}
