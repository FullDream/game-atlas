import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'

const attachmentVariants = cva(
	'bg-card text-card-foreground rounded-xl shadow-xs group/attachment focus-within:ring-ring/50 has-[>a,>button]:hover:bg-muted/50 data-[state=error]:border-destructive/30 min-w-0 relative flex w-fit max-w-full shrink-0 flex-wrap border transition-colors focus-within:ring-1 data-[state=idle]:border-dashed',
	{
		variants: {
			size: {
				default:
					'gap-2 text-sm has-data-[slot=attachment-content]:px-2.5 has-data-[slot=attachment-content]:py-2 has-data-[slot=attachment-media]:p-2',
				sm: 'gap-2.5 text-xs has-data-[slot=attachment-content]:px-2 has-data-[slot=attachment-content]:py-1.5 has-data-[slot=attachment-media]:p-1.5',
				xs: 'rounded-md gap-1.5 text-xs has-data-[slot=attachment-content]:px-1.5 has-data-[slot=attachment-content]:py-1 has-data-[slot=attachment-media]:p-1',
			},
			orientation: {
				horizontal: 'min-w-40 items-center',
				vertical: 'w-24 has-data-[slot=attachment-content]:w-30 flex-col',
			},
		},
		defaultVariants: {
			size: 'default',
			orientation: 'horizontal',
		},
	},
)

export type AttachmentVariants = VariantProps<typeof attachmentVariants>
export type AttachmentState = 'idle' | 'uploading' | 'processing' | 'error' | 'done'

@Directive({
	selector: '[hlmAttachment],hlm-attachment',
	exportAs: 'hlmAttachment',
	host: {
		'data-slot': 'attachment',
		'[attr.data-state]': 'state()',
		'[attr.data-size]': 'size()',
		'[attr.data-orientation]': 'orientation()',
	},
})
export class HlmAttachment {
	readonly state = input<AttachmentState>('done')
	readonly size = input<AttachmentVariants['size']>('default')
	readonly orientation = input<AttachmentVariants['orientation']>('horizontal')

	constructor() {
		classes(() => attachmentVariants({ size: this.size(), orientation: this.orientation() }))
	}
}
