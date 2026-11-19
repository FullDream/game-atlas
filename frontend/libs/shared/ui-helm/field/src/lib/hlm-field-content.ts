import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmFieldContent],hlm-field-content',
	exportAs: 'hlmFieldContent',
	host: { 'data-slot': 'field-content' },
})
export class HlmFieldContent {
	constructor() {
		classes(() => 'gap-1 group/field-content leading-snug flex flex-1 flex-col')
	}
}
