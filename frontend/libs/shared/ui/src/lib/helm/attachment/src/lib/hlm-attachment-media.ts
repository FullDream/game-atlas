import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'

const attachmentMediaVariants = cva(
	"bg-muted text-foreground w-10 rounded-md group-data-[state=error]/attachment:bg-destructive/10 group-data-[state=error]/attachment:text-destructive group-data-[size=sm]/attachment:w-8 group-data-[size=xs]/attachment:w-7 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] group-data-[orientation=vertical]/attachment:[&_ng-icon:not([class*='text-'])]:text-[length:--spacing(6)] group-data-[size=xs]/attachment:[&_ng-icon:not([class*='text-'])]:text-[length:--spacing(3.5)] relative flex aspect-square shrink-0 items-center justify-center overflow-hidden group-data-[orientation=vertical]/attachment:w-full [&_ng-icon]:pointer-events-none",
	{
		variants: {
			variant: {
				icon: 'bg-muted',
				image: 'opacity-60 group-data-[state=done]/attachment:opacity-100 group-data-[state=idle]/attachment:opacity-100 *:[img]:aspect-square *:[img]:w-full *:[img]:object-cover',
			},
		},
		defaultVariants: {
			variant: 'icon',
		},
	},
)

export type AttachmentMediaVariants = VariantProps<typeof attachmentMediaVariants>

@Directive({
	selector: '[hlmAttachmentMedia],hlm-attachment-media',
	exportAs: 'hlmAttachmentMedia',
	host: {
		'data-slot': 'attachment-media',
		'[attr.data-variant]': 'variant()',
	},
})
export class HlmAttachmentMedia {
	readonly variant = input<AttachmentMediaVariants['variant']>('icon')

	constructor() {
		classes(() => attachmentMediaVariants({ variant: this.variant() }))
	}
}
