import { Component } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCheckRound } from '@ng-icons/material-symbols/round'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-dropdown-menu-checkbox-indicator',
	exportAs: 'hlmDropdownMenuCheckboxIndicator',
	imports: [NgIcon],
	providers: [provideIcons({ matCheckRound })],
	host: { 'data-slot': 'dropdown-menu-checkbox-item-indicator' },
	template: `
		<ng-icon name="matCheckRound" />
	`,
})
export class HlmDropdownMenuCheckboxIndicator {
	constructor() {
		classes(
			() =>
				'end-2 [&_ng-icon]:text-[length:--spacing(4)] pointer-events-none absolute flex items-center justify-center opacity-0 group-data-checked/dropdown-menu-checkbox:opacity-100',
		)
	}
}
