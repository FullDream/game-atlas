import { Directive, input } from '@angular/core'
import { BrnTabs } from '@spartan-ng/brain/tabs'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmTabs],hlm-tabs',
	exportAs: 'hlmTabs',
	hostDirectives: [
		{
			directive: BrnTabs,
			inputs: ['orientation', 'activationMode', 'brnTabs: tab'],
			outputs: ['tabActivated'],
		},
	],
	host: {
		'data-slot': 'tabs',
	},
})
export class HlmTabs {
	readonly tab = input.required<string>()

	constructor() {
		classes(() => 'group/tabs gap-2 flex data-[orientation=horizontal]:flex-col')
	}
}
