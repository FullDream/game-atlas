import { Component, inject } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCheckRound } from '@ng-icons/material-symbols/round'
import { BrnSelectItem } from '@spartan-ng/brain/select'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-select-item',
	exportAs: 'hlmSelectItem',
	imports: [NgIcon],
	templateUrl: './hlm-select-item.html',
	providers: [provideIcons({ matCheckRound })],
	hostDirectives: [{ directive: BrnSelectItem, inputs: ['id', 'disabled', 'value'] }],
	host: { 'data-slot': 'select-item' },
})
export class HlmSelectItem {
	readonly #brnSelectItem = inject(BrnSelectItem)

	protected readonly active = this.#brnSelectItem.active

	constructor() {
		classes(
			() =>
				'data-highlighted:bg-accent data-highlighted:text-accent-foreground not-data-[variant=destructive]:data-highlighted:**:text-accent-foreground gap-2 rounded-sm py-1.5 ps-2 pe-8 text-sm *:[span]:last:gap-2 relative flex w-full cursor-default items-center outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 [&_ng-icon]:pointer-events-none [&_ng-icon]:shrink-0 *:[span]:last:flex *:[span]:last:items-center',
		)
	}
}
