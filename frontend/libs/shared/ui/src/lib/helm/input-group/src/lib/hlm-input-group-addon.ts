import { Directive, input } from '@angular/core'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'

const inputGroupAddonVariants = cva(
	"text-muted-foreground gap-2 py-1.5 text-sm font-medium [&>ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] flex h-auto cursor-text items-center justify-center select-none group-data-[disabled=true]/input-group:opacity-50 [&>kbd]:rounded-[calc(var(--radius)-5px)]",
	{
		variants: {
			align: {
				'inline-start': 'ps-2 has-[>button]:-ms-1 order-first has-[>kbd]:ms-[-0.15rem]',
				'inline-end': 'pe-2 has-[>button]:-me-1 order-last has-[>kbd]:me-[-0.15rem]',
				'block-start':
					'px-2.5 pt-2 group-has-[>input]/input-group:pt-2 [.border-b]:pb-2 order-first w-full justify-start',
				'block-end':
					'px-2.5 pb-2 group-has-[>input]/input-group:pb-2 [.border-t]:pt-2 order-last w-full justify-start',
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
