import { Component, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMoreHorizRound } from '@ng-icons/material-symbols/round'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-pagination-ellipsis',
	exportAs: 'hlmPaginationEllipsis',
	imports: [NgIcon],
	providers: [provideIcons({ matMoreHorizRound })],
	host: { 'data-slot': 'pagination-ellipsis' },
	template: `
		<ng-icon name="matMoreHorizRound" aria-hidden="true" />
		<span class="sr-only">{{ srOnlyText() }}</span>
	`,
})
export class HlmPaginationEllipsis {
	/** Screen reader only text for the ellipsis */
	readonly srOnlyText = input<string>('More pages')

	constructor() {
		classes(
			() =>
				"size-9 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] flex items-center justify-center",
		)
	}
}
