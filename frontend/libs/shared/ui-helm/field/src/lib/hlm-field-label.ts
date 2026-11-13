import { Directive } from '@angular/core'
import { HlmLabel } from '@spartan-ng/helm/label'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmFieldLabel],hlm-field-label',
	exportAs: 'hlmFieldLabel',
	hostDirectives: [HlmLabel],
	host: { 'data-slot': 'field-label' },
})
export class HlmFieldLabel {
	constructor() {
		classes(() => [
			'has-data-checked:bg-primary/5 has-data-checked:border-primary/30 dark:has-data-checked:border-primary/20 dark:has-data-checked:bg-primary/10 gap-2 leading-snug has-[>[data-slot=field]]:rounded-xl *:data-[slot=field]:p-4 group/field-label peer/field-label flex w-fit group-data-[disabled=true]/field:opacity-50 has-[>[data-slot=field]]:border',
			'has-[>[data-slot=field]]:w-full has-[>[data-slot=field]]:flex-col',
		])
	}
}
