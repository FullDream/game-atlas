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
				'h-9 min-w-0 rounded-md border-input px-2.5 py-1 text-base shadow-xs file:h-7 file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:border-destructive data-[matches-spartan-invalid=true]:ring-destructive/20 md:text-sm dark:bg-input/30 dark:data-[matches-spartan-invalid=true]:border-destructive/50 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 w-full border bg-transparent transition-[color,box-shadow] outline-none file:inline-flex file:border-0 file:bg-transparent focus-visible:ring-3 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3',
		)
	}
}
