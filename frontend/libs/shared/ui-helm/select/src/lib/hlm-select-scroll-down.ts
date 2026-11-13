import { Component } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matKeyboardArrowDownRound } from '@ng-icons/material-symbols/round'
import { BrnSelectScrollDown } from '@spartan-ng/brain/select'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-select-scroll-down',
	exportAs: 'hlmSelectScrollDown',
	imports: [NgIcon],
	providers: [provideIcons({ matKeyboardArrowDownRound })],
	hostDirectives: [BrnSelectScrollDown],
	template: `
		<ng-icon name="matKeyboardArrowDownRound" aria-hidden="true" />
	`,
})
export class HlmSelectScrollDown {
	constructor() {
		classes(
			() =>
				"bg-popover py-1 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] bottom-0 sticky z-10 flex w-full cursor-default items-center justify-center data-hidden:hidden",
		)
	}
}
