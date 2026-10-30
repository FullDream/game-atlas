import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmAlertTitle]',
	exportAs: 'hlmAlertTitle',
	host: {
		'data-slot': 'alert-title',
	},
})
export class HlmAlertTitle {
	constructor() {
		classes(
			() =>
				'font-medium [&_a]:hover:text-foreground group-has-[>ng-icon]/alert:col-start-2 [&_a]:underline [&_a]:underline-offset-3',
		)
	}
}
