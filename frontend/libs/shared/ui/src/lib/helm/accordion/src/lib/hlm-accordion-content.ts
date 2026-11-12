import { Component } from '@angular/core'
import { BrnAccordionContent } from '@spartan-ng/brain/accordion'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-accordion-content',
	exportAs: 'hlmAccordionContent',
	hostDirectives: [{ directive: BrnAccordionContent, inputs: ['style'] }],
	host: {
		'data-slot': 'accordion-content',
	},
	template: `
		<div
			class="pt-0 pb-4 [&_a]:hover:text-foreground [&_p:not(:last-child)]:mb-4 [&_a]:underline [&_a]:underline-offset-3"
		>
			<ng-content />
		</div>
	`,
})
export class HlmAccordionContent {
	constructor() {
		classes(
			() =>
				'px-4 text-sm data-[state=closed]:h-0 transition-all data-[state=open]:h-(--brn-accordion-content-height)',
		)
	}
}
