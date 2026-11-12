import { Directive } from '@angular/core'
import { BrnFieldControlDescribedBy } from '@spartan-ng/brain/field'
import { BrnInput } from '@spartan-ng/brain/input'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmInput]',
	exportAs: 'hlmInput',
	hostDirectives: [{ directive: BrnInput, inputs: ['id', 'forceInvalid'] }, BrnFieldControlDescribedBy],
	host: { 'data-slot': 'input' },
})
export class HlmInput {
	constructor() {
		classes(
			() =>
				'bg-input/30 border-input focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:ring-destructive/20 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 data-[matches-spartan-invalid=true]:border-destructive dark:data-[matches-spartan-invalid=true]:border-destructive/50 h-9 rounded-4xl px-3 py-1 text-base file:h-7 file:text-sm file:font-medium md:text-sm file:text-foreground placeholder:text-muted-foreground min-w-0 w-full border transition-colors outline-none file:inline-flex file:border-0 file:bg-transparent focus-visible:ring-3 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3',
		)
	}
}
