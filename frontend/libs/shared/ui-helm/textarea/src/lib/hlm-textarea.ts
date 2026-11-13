import { Directive } from '@angular/core'
import { BrnFieldControlDescribedBy } from '@spartan-ng/brain/field'
import { BrnTextarea } from '@spartan-ng/brain/textarea'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmTextarea]',
	exportAs: 'hlmTextarea',
	hostDirectives: [{ directive: BrnTextarea, inputs: ['id', 'forceInvalid'] }, BrnFieldControlDescribedBy],
	host: { 'data-slot': 'textarea' },
})
export class HlmTextarea {
	constructor() {
		classes(
			() =>
				'border-input bg-input/30 focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:ring-destructive/20 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 data-[matches-spartan-invalid=true]:border-destructive dark:data-[matches-spartan-invalid=true]:border-destructive/50 rounded-xl px-3 py-3 text-base md:text-sm placeholder:text-muted-foreground min-h-16 flex field-sizing-content w-full resize-none border transition-colors outline-none focus-visible:ring-3 disabled:cursor-not-allowed disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3',
		)
	}
}
