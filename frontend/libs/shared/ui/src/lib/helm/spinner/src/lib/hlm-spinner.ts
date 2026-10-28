import { Component, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matProgressActivityRound } from '@ng-icons/material-symbols/round'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-spinner',
	exportAs: 'hlmSpinner',
	imports: [NgIcon],
	providers: [provideIcons({ matProgressActivityRound })],
	host: {
		'data-slot': 'spinner',
		role: 'status',
		'[attr.aria-label]': 'ariaLabel()',
	},
	template: `
		<ng-icon [name]="icon()" size="100%" class="motion-safe:animate-spin" />
	`,
})
export class HlmSpinner {
	/**
	 * The name of the icon to be used as the spinner.
	 * Use provideIcons({ ... }) to register custom icons.
	 */
	public readonly icon = input<string>('matProgressActivityRound')

	/** Aria label for the spinner for accessibility. */
	public readonly ariaLabel = input<string>('Loading', { alias: 'aria-label' })

	constructor() {
		classes(() => 'size-4 inline-flex shrink-0 items-center justify-center')
	}
}
