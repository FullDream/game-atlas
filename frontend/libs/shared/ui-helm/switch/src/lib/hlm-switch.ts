import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, forwardRef, input, linkedSignal, output } from '@angular/core'
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms'
import type { ChangeFn, TouchFn } from '@spartan-ng/brain/forms'
import { BrnSwitch, type BrnSwitchSize } from '@spartan-ng/brain/switch'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'
import { HlmSwitchThumb } from './hlm-switch-thumb'

export const HLM_SWITCH_VALUE_ACCESSOR = {
	provide: NG_VALUE_ACCESSOR,
	useExisting: forwardRef(() => HlmSwitch),
	multi: true,
}

@Component({
	selector: 'hlm-switch',
	exportAs: 'hlmSwitch',
	imports: [BrnSwitch, HlmSwitchThumb],
	providers: [HLM_SWITCH_VALUE_ACCESSOR],
	host: {
		'data-slot': 'switch',
		class: 'contents',
		'[attr.aria-label]': 'null',
		'[attr.aria-labelledby]': 'null',
		'[attr.aria-describedby]': 'null',
	},
	template: `
		<brn-switch
			[class]="computedClass()"
			[size]="size()"
			[checked]="checked()"
			[disabled]="internalDisabled()"
			[id]="inputId()"
			[forceInvalid]="forceInvalid()"
			[aria-label]="ariaLabel()"
			[aria-labelledby]="ariaLabelledby()"
			[aria-describedby]="ariaDescribedby()"
			(checkedChange)="handleChange($event)"
			(touched)="onTouched?.()"
		>
			<hlm-switch-thumb />
		</brn-switch>
	`,
})
export class HlmSwitch implements ControlValueAccessor {
	readonly userClass = input<ClassValue>('', { alias: 'class' })

	/** The checked state of the switch. */
	readonly checkedInput = input<boolean, BooleanInput>(false, { alias: 'checked', transform: booleanAttribute })

	/** The disabled state of the switch. */
	readonly disabled = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})

	/** The size of the switch. */
	readonly size = input<BrnSwitchSize>('default')

	/** Used to set the id on the underlying brn element. */
	readonly inputId = input<string | null>(null)

	/** Whether to force the input into an invalid state. */
	readonly forceInvalid = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	/** Used to set the aria-label attribute on the underlying brn element. */
	readonly ariaLabel = input<string | null>(null, { alias: 'aria-label' })

	/** Used to set the aria-labelledby attribute on the underlying brn element. */
	readonly ariaLabelledby = input<string | null>(null, { alias: 'aria-labelledby' })

	/** Used to set the aria-describedby attribute on the underlying brn element. */
	readonly ariaDescribedby = input<string | null>(null, { alias: 'aria-describedby' })

	/** Emits when the checked state of the switch changes. */
	readonly checkedChange = output<boolean>()

	protected onChange?: ChangeFn<boolean>
	protected onTouched?: TouchFn

	readonly checked = linkedSignal(this.checkedInput)
	protected readonly internalDisabled = linkedSignal(this.disabled)
	protected readonly computedClass = computed(() =>
		hlm(
			'data-checked:bg-primary data-unchecked:bg-input focus-visible:border-ring focus-visible:ring-ring/50 data-[matches-spartan-invalid=true]:ring-destructive/20 dark:data-[matches-spartan-invalid=true]:ring-destructive/40 data-[matches-spartan-invalid=true]:border-destructive dark:data-[matches-spartan-invalid=true]:border-destructive/50 dark:data-unchecked:bg-input/80 data-[size=default]:w-8 data-[size=sm]:h-3.5 data-[size=sm]:w-6 group/switch inline-flex shrink-0 items-center rounded-full border border-transparent transition-all outline-none focus-visible:ring-3 data-disabled:cursor-not-allowed data-disabled:opacity-50 data-[matches-spartan-invalid=true]:ring-3 data-[size=default]:h-[18.4px]',
			this.userClass(),
		),
	)

	/** CONTROL VALUE ACCESSOR */
	writeValue(value: boolean): void {
		this.checked.set(Boolean(value))
	}

	registerOnChange(fn: ChangeFn<boolean>): void {
		this.onChange = fn
	}

	registerOnTouched(fn: TouchFn): void {
		this.onTouched = fn
	}

	setDisabledState(isDisabled: boolean): void {
		this.internalDisabled.set(isDisabled)
	}

	protected handleChange(value: boolean): void {
		this.checked.set(value)
		this.onChange?.(value)
		this.checkedChange.emit(value)
	}
}
