import { Component, input } from '@angular/core'
import { NgIcon, type IconType } from '@ng-icons/core'
import { classes } from '@spartan-ng/helm/utils'

type ErrorStateVariant = 'default' | 'compact'

@Component({
	selector: 'ui-error-state',
	imports: [NgIcon],
	templateUrl: './error-state.html',
	host: {
		role: 'alert',
		'data-slot': 'error-state',
		'[attr.data-variant]': 'variant()',
	},
})
export class ErrorState {
	readonly icon = input<IconType>()
	readonly variant = input<ErrorStateVariant>('default')

	constructor() {
		classes(() => [
			'group/error-state gap-4 rounded-xl flex border',
			this.variant() === 'compact'
				? 'border-destructive/20 bg-destructive/5 p-4 flex-wrap items-center'
				: 'min-h-64 px-6 py-10 flex-col items-center justify-center border-dashed text-center',
		])
	}
}
