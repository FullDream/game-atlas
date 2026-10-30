import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmAlertAction]',
	exportAs: 'hlmAlertAction',
	host: {
		'data-slot': 'alert-action',
	},
})
export class HlmAlertAction {
	constructor() {
		classes(() => 'end-3 top-2.5 absolute')
	}
}
