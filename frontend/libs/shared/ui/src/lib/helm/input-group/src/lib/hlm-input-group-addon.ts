import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'

const inputGroupAddonVariants = cva(
	"text-muted-foreground **:data-[slot=kbd]:bg-muted-foreground/10 gap-2 py-2 text-sm font-medium **:data-[slot=kbd]:rounded-4xl **:data-[slot=kbd]:px-1.5 [&>ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] flex h-auto cursor-text items-center justify-center select-none group-data-[disabled=true]/input-group:opacity-50",
	{
		variants: {
			align: {
				'inline-start': 'ps-3 has-[>button]:-ms-1 order-first has-[>kbd]:ms-[-0.15rem]',
				'inline-end': 'pe-3 has-[>button]:-me-1 order-last has-[>kbd]:me-[-0.15rem]',
				'block-start':
					'px-3 pt-3 group-has-[>input]/input-group:pt-3 [.border-b]:pb-3 order-first w-full justify-start',
				'block-end':
					'px-3 pb-3 group-has-[>input]/input-group:pb-3 [.border-t]:pt-3 order-last w-full justify-start',
			},
		},
		defaultVariants: {
			align: 'inline-start',
		},
	},
)

type InputGroupAddonVariants = VariantProps<typeof inputGroupAddonVariants>

@Directive({
	selector: '[hlmInputGroupAddon],hlm-input-group-addon',
	exportAs: 'hlmInputGroupAddon',
	host: {
		role: 'group',
		'data-slot': 'input-group-addon',
		'[attr.data-align]': 'align()',
	},
})
export class HlmInputGroupAddon {
	readonly align = input<InputGroupAddonVariants['align']>('inline-start')

	constructor() {
		classes(() => inputGroupAddonVariants({ align: this.align() }))
	}
}
