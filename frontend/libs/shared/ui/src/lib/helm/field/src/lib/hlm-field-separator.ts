import { Component } from '@angular/core'
import { HlmSeparator } from '@spartan-ng/helm/separator'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-field-separator',
	exportAs: 'hlmFieldSeparator',
	imports: [HlmSeparator],
	templateUrl: './hlm-field-separator.html',
	host: { 'data-slot': 'field-separator' },
})
export class HlmFieldSeparator {
	constructor() {
		classes(() => '-my-2 h-5 text-sm group-data-[variant=outline]/field-group:-mb-2 relative')
	}
}
