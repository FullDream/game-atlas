import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmEmpty],hlm-empty',
	exportAs: 'hlmEmpty',
	host: { 'data-slot': 'empty' },
})
export class HlmEmpty {
	constructor() {
		classes(
			() =>
				'gap-4 rounded-lg p-12 min-w-0 flex w-full flex-1 flex-col items-center justify-center border-dashed text-center text-balance',
		)
	}
}
