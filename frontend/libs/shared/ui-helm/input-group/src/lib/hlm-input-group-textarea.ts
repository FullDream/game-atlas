import { Directive, inject } from '@angular/core'
import { BrnTextarea } from '@spartan-ng/brain/textarea'
import { HlmTextarea } from '@spartan-ng/helm/textarea'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: 'textarea[hlmInputGroupTextarea]',
	exportAs: 'hlmInputGroupTextarea',
	hostDirectives: [HlmTextarea],
	host: { 'data-slot': 'input-group-control' },
})
export class HlmInputGroupTextarea {
	readonly id = inject(BrnTextarea).id

	constructor() {
		classes(
			() =>
				'py-2 flex-1 resize-none rounded-none border-0 bg-transparent shadow-none ring-0 focus-visible:ring-0 data-[matches-spartan-invalid=true]:ring-0 dark:bg-transparent',
		)
	}
}
