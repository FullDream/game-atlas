import { Directive, input, signal } from '@angular/core'
import { BrnButton } from '@spartan-ng/brain/button'
import { classes } from '@spartan-ng/helm/utils'
import { cva, type VariantProps } from 'class-variance-authority'
import type { ClassValue } from 'clsx'
import { injectBrnButtonConfig } from './hlm-button.token'

export const buttonVariants = cva(
	"group/button rounded-md text-sm font-medium focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:border-destructive data-[matches-spartan-invalid=true]:ring-destructive/20 dark:data-[matches-spartan-invalid=true]:border-destructive/50 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(5)] inline-flex shrink-0 items-center justify-center border border-transparent bg-clip-padding whitespace-nowrap transition-all outline-none select-none focus-visible:ring-3 active:not-aria-[haspopup]:translate-y-px data-disabled:pointer-events-none data-disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3 [&_ng-icon]:pointer-events-none [&_ng-icon]:shrink-0",
	{
		variants: {
			variant: {
				default: 'bg-primary text-primary-foreground hover:bg-primary/80',
				outline:
					'border-border bg-background shadow-xs hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50',
				secondary:
					'bg-secondary text-secondary-foreground aria-expanded:bg-secondary aria-expanded:text-secondary-foreground hover:bg-[color-mix(in_oklch,var(--secondary),var(--foreground)_5%)]',
				ghost: 'hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:hover:bg-muted/50',
				destructive:
					'bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40',
				link: 'text-primary underline-offset-4 hover:underline',
			},
			size: {
				default:
					'h-9 gap-2 px-2.5 in-data-[slot=button-group]:rounded-md has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2',
				xs: "h-6 gap-1 px-2 text-xs in-data-[slot=button-group]:rounded-md has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(3)] rounded-[min(var(--radius-md),8px)]",
				sm: 'h-8 gap-1.5 px-2.5 in-data-[slot=button-group]:rounded-md has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 rounded-[min(var(--radius-md),10px)]',
				lg: 'h-10 gap-2 px-2.5 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2',
				icon: 'size-9',
				'icon-xs':
					"size-6 in-data-[slot=button-group]:rounded-md [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(3)] rounded-[min(var(--radius-md),8px)]",
				'icon-sm': 'size-8 in-data-[slot=button-group]:rounded-md rounded-[min(var(--radius-md),10px)]',
				'icon-lg': 'size-10',
			},
		},
		defaultVariants: {
			variant: 'default',
			size: 'default',
		},
	},
)

export type ButtonVariants = VariantProps<typeof buttonVariants>

@Directive({
	selector: 'button[hlmBtn], a[hlmBtn]',
	exportAs: 'hlmBtn',
	hostDirectives: [{ directive: BrnButton, inputs: ['disabled'] }],
	host: { 'data-slot': 'button' },
})
export class HlmButton {
	readonly #config = injectBrnButtonConfig()

	readonly #additionalClasses = signal<ClassValue>('')

	public readonly variant = input<ButtonVariants['variant']>(this.#config.variant)

	public readonly size = input<ButtonVariants['size']>(this.#config.size)

	constructor() {
		classes(() => [buttonVariants({ variant: this.variant(), size: this.size() }), this.#additionalClasses()])
	}

	setClass(classes: string): void {
		this.#additionalClasses.set(classes)
	}
}
