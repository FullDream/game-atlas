import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmFieldTitle],hlm-field-title',
	exportAs: 'hlmFieldTitle',
	host: { 'data-slot': 'field-label' },
})
export class HlmFieldTitle {
	constructor() {
		classes(
			() =>
				'gap-2 text-sm leading-snug font-medium flex w-fit items-center group-data-[disabled=true]/field:opacity-50',
		)
	}
}
