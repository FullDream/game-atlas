import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { _IdGenerator as IdGenerator } from '@angular/cdk/a11y'
import { Component, computed, inject, input, model } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matDarkModeRound, matDesktopWindowsRound, matLightModeRound } from '@ng-icons/material-symbols/round'
import type { ThemePreference } from './theme-preference'

@Component({
	selector: 'ui-theme-switcher',
	imports: [HlmTooltip, NgIcon],
	templateUrl: './theme-switcher.html',
	providers: [provideIcons({ matDarkModeRound, matDesktopWindowsRound, matLightModeRound })],
	host: {
		'[attr.role]': 'compact() ? null : "radiogroup"',
		'aria-label': 'Color theme',
		class: 'inline-flex shrink-0 items-center gap-1 rounded-4xl border p-1 has-focus-visible:border-ring has-focus-visible:ring-2 has-focus-visible:ring-ring/50',
	},
})
export class ThemeSwitcher {
	readonly value = model<ThemePreference>('system')
	readonly compact = input(false)

	protected readonly groupName = inject(IdGenerator).getId('theme-')

	protected readonly options = [
		{ value: 'light', label: 'Light theme', icon: 'matLightModeRound' },
		{ value: 'system', label: 'Default theme (system)', icon: 'matDesktopWindowsRound' },
		{ value: 'dark', label: 'Dark theme', icon: 'matDarkModeRound' },
	] satisfies { value: ThemePreference; label: string; icon: string }[]

	protected readonly active = computed(
		() => this.options.find(option => option.value === this.value()) ?? this.options[1],
	)

	protected cycle(): void {
		const index = this.options.findIndex(option => option.value === this.value())

		this.value.set(this.options[(index + 1) % this.options.length].value)
	}
}
