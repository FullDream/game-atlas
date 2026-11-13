import { Component } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matKeyboardArrowUpRound } from '@ng-icons/material-symbols/round'
import { BrnSelectScrollUp } from '@spartan-ng/brain/select'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-select-scroll-up',
	exportAs: 'hlmSelectScrollUp',
	imports: [NgIcon],
	providers: [provideIcons({ matKeyboardArrowUpRound })],
	hostDirectives: [BrnSelectScrollUp],
	template: `
		<ng-icon name="matKeyboardArrowUpRound" aria-hidden="true" />
	`,
})
export class HlmSelectScrollUp {
	constructor() {
		classes(
			() =>
				"bg-popover py-1 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] top-0 sticky z-10 flex w-full cursor-default items-center justify-center data-hidden:hidden",
		)
	}
}
