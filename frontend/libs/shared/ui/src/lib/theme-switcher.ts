import { _IdGenerator as IdGenerator } from '@angular/cdk/a11y'
import { Component, inject, model } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matDarkModeRound, matDesktopWindowsRound, matLightModeRound } from '@ng-icons/material-symbols/round'
import type { ThemePreference } from './theme-preference'

@Component({
	selector: 'ui-theme-switcher',
	imports: [NgIcon],
	providers: [provideIcons({ matDarkModeRound, matDesktopWindowsRound, matLightModeRound })],
	host: {
		role: 'radiogroup',
		'aria-label': 'Color theme',
		class: 'inline-flex shrink-0 items-center gap-1 rounded-lg border p-1 has-focus-visible:border-ring has-focus-visible:ring-2 has-focus-visible:ring-ring/50',
	},
	template: `
		@for (option of options; track option.value) {
			<label
				class="size-8 rounded-md text-sm hover:bg-accent hover:text-accent-foreground has-checked:bg-secondary has-checked:text-secondary-foreground has-focus-visible:ring-ring/50 relative flex cursor-pointer items-center justify-center transition-colors has-focus-visible:ring-2"
				[title]="option.label"
			>
				<input
					type="radio"
					class="sr-only"
					[name]="groupName"
					[value]="option.value"
					[checked]="value() === option.value"
					(change)="value.set(option.value)"
				/>
				<ng-icon [name]="option.icon" />
				<span class="sr-only">{{ option.label }}</span>
			</label>
		}
	`,
})
export class ThemeSwitcher {
	readonly value = model<ThemePreference>('system')

	protected readonly groupName = inject(IdGenerator).getId('theme-')

	protected readonly options = [
		{ value: 'light', label: 'Light theme', icon: 'matLightModeRound' },
		{ value: 'system', label: 'Default theme (system)', icon: 'matDesktopWindowsRound' },
		{ value: 'dark', label: 'Dark theme', icon: 'matDarkModeRound' },
	] satisfies { value: ThemePreference; label: string; icon: string }[]
}
