import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'

const alertVariants = cva(
	"gap-0.5 rounded-lg px-4 py-3 text-sm has-data-[slot=alert-action]:pe-18 has-[>ng-icon]:gap-x-2.5 *:[ng-icon]:translate-y-0.5 *:[ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] group/alert relative grid w-full border text-start has-data-[slot=alert-action]:relative has-[>ng-icon]:grid-cols-[auto_1fr] *:[ng-icon]:row-span-2 *:[ng-icon]:text-current",
	{
		variants: {
			variant: {
				default: 'bg-card text-card-foreground',
				destructive:
					'text-destructive bg-card *:data-[slot=alert-description]:text-destructive/90 *:[ng-icon]:text-current',
			},
		},
		defaultVariants: {
			variant: 'default',
		},
	},
)

export type AlertVariants = VariantProps<typeof alertVariants>

@Directive({
	selector: 'hlm-alert,[hlmAlert]',
	exportAs: 'hlmAlert',
	host: {
		'data-slot': 'alert',
		role: 'alert',
	},
})
export class HlmAlert {
	readonly variant = input<AlertVariants['variant']>('default')

	constructor() {
		classes(() => alertVariants({ variant: this.variant() }))
	}
}
