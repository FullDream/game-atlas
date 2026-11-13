import { Directive, input } from '@angular/core'
import { BrnTabsList } from '@spartan-ng/brain/tabs'
import { classes } from '@spartan-ng/helm/utils'
import { type VariantProps, cva } from 'class-variance-authority'

export const listVariants = cva(
	'rounded-4xl group-data-horizontal/tabs:h-9 group-data-vertical/tabs:rounded-2xl group/tabs-list text-muted-foreground p-0.75 inline-flex w-fit items-center justify-center group-data-[orientation=vertical]/tabs:h-fit group-data-[orientation=vertical]/tabs:flex-col data-[variant=line]:rounded-none',
	{
		variants: {
			variant: {
				default: 'bg-muted',
				line: 'gap-1 bg-transparent',
			},
		},
		defaultVariants: {
			variant: 'default',
		},
	},
)
type ListVariants = VariantProps<typeof listVariants>

@Directive({
	selector: '[hlmTabsList],hlm-tabs-list',
	exportAs: 'hlmTabsList',
	hostDirectives: [BrnTabsList],
	host: {
		'data-slot': 'tabs-list',
		'[attr.data-variant]': 'variant()',
	},
})
export class HlmTabsList {
	readonly variant = input<ListVariants['variant']>('default')

	constructor() {
		classes(() => listVariants({ variant: this.variant() }))
	}
}
