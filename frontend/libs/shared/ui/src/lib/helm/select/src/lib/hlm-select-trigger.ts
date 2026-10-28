import { _IdGenerator as IdGenerator } from '@angular/cdk/a11y'
import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matKeyboardArrowDownRound } from '@ng-icons/material-symbols/round'
import { BrnFieldControlDescribedBy } from '@spartan-ng/brain/field'
import { BrnSelectTrigger } from '@spartan-ng/brain/select'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'

@Component({
	selector: 'hlm-select-trigger',
	exportAs: 'hlmSelectTrigger',
	imports: [NgIcon, BrnSelectTrigger, BrnFieldControlDescribedBy],
	templateUrl: './hlm-select-trigger.html',
	providers: [provideIcons({ matKeyboardArrowDownRound })],
})
export class HlmSelectTrigger {
	readonly #idGenerator = inject(IdGenerator)

	readonly userClass = input<ClassValue>('', { alias: 'class' })
	protected readonly computedClass = computed(() =>
		hlm(
			'border-input data-placeholder:text-muted-foreground dark:bg-input/30 dark:hover:bg-input/50 focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:ring-destructive/20 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 data-[matches-spartan-invalid=true]:border-destructive dark:data-[matches-spartan-invalid=true]:border-destructive/50 gap-1.5 rounded-md py-2 ps-2.5 pe-2 text-sm shadow-xs data-[size=default]:h-9 data-[size=sm]:h-8 *:data-[slot=select-value]:gap-1.5 flex w-fit items-center justify-between border bg-transparent whitespace-nowrap transition-[color,box-shadow] outline-none focus-visible:ring-3 disabled:cursor-not-allowed disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3 *:data-[slot=select-value]:line-clamp-1 *:data-[slot=select-value]:flex *:data-[slot=select-value]:items-center [&_ng-icon]:pointer-events-none [&_ng-icon]:shrink-0',
			this.userClass(),
		),
	)

	readonly buttonId = input<string>(this.#idGenerator.getId('hlm-select-trigger-'))

	readonly size = input<'default' | 'sm'>('default')

	readonly ariaLabel = input<string | undefined>(undefined, { alias: 'aria-label' })

	/** Whether to force the trigger into an invalid state. */
	readonly forceInvalid = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	/** Manual override for aria-invalid. When not set, auto-detects from the parent autocomplete error state. */
	readonly ariaInvalidInput = input<boolean | undefined, BooleanInput>(undefined, {
		transform: (v: BooleanInput) => (v === '' || v === undefined ? undefined : booleanAttribute(v)),
		alias: 'aria-invalid',
	})
}
