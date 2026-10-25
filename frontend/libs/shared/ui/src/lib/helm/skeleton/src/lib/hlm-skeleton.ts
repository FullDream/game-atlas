import { Directive } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmSkeleton],hlm-skeleton',
	exportAs: 'hlmSkeleton',
	host: {
		'data-slot': 'skeleton',
	},
})
export class HlmSkeleton {
	constructor() {
		classes(() => 'block rounded-md bg-muted motion-safe:animate-pulse')
	}
}
