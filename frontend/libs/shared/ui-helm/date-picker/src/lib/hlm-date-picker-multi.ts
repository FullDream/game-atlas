import type { BooleanInput, NumberInput } from '@angular/cdk/coercion'
import {
	booleanAttribute,
	Component,
	computed,
	contentChild,
	forwardRef,
	input,
	linkedSignal,
	numberAttribute,
	output,
	signal,
	viewChild,
} from '@angular/core'
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms'
import { type BrnDatePickerBase, BrnDatePickerTriggerToken, provideBrnDatePicker } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl, provideBrnLabelable } from '@spartan-ng/brain/field'
import type { ChangeFn, TouchFn } from '@spartan-ng/brain/forms'
import type { BrnOverlayState } from '@spartan-ng/brain/overlay'
import { BrnPopover, type BrnPopoverAlign } from '@spartan-ng/brain/popover'
import { HlmCalendarMulti } from '@spartan-ng/helm/calendar'
import { HlmPopoverImports } from '@spartan-ng/helm/popover'
import { injectHlmDatePickerMultiConfig } from './hlm-date-picker-multi.token'

export const HLM_DATE_PICKER_MULTI_VALUE_ACCESSOR = {
	provide: NG_VALUE_ACCESSOR,
	useExisting: forwardRef(() => HlmDatePickerMulti),
	multi: true,
}

@Component({
	selector: 'hlm-date-picker-multi',
	exportAs: 'hlmDatePickerMulti',
	imports: [HlmPopoverImports, HlmCalendarMulti],
	templateUrl: './hlm-date-picker-multi.html',
	providers: [
		HLM_DATE_PICKER_MULTI_VALUE_ACCESSOR,
		provideBrnDatePicker(HlmDatePickerMulti),
		provideBrnLabelable(HlmDatePickerMulti),
	],
	hostDirectives: [BrnFieldControl],
	host: { class: 'block' },
})
export class HlmDatePickerMulti<T> implements BrnDatePickerBase<T[]>, ControlValueAccessor {
	readonly #config = injectHlmDatePickerMultiConfig<T>()

	readonly popover = viewChild.required(BrnPopover)

	protected readonly trigger = contentChild(BrnDatePickerTriggerToken)

	readonly align = input<BrnPopoverAlign>('center')

	/** Show dropdowns to navigate between months or years. */
	readonly captionLayout = input<'dropdown' | 'label' | 'dropdown-months' | 'dropdown-years'>('label')

	/** The minimum date that can be selected.*/
	readonly minDate = input<T>()

	/** The maximum date that can be selected. */
	readonly maxDate = input<T>()

	/** The minimum selectable dates.  */
	readonly minSelection = input<number, NumberInput>(undefined, {
		transform: numberAttribute,
	})

	/** The maximum selectable dates.  */
	readonly maxSelection = input<number, NumberInput>(undefined, {
		transform: numberAttribute,
	})

	/** Determine if the date picker is disabled. */
	readonly disabled = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})

	/** The selected value. */
	readonly date = input<T[]>()

	protected readonly mutableDate = linkedSignal(this.date)

	/** If true, the date picker will close when the max selection of dates is reached. */
	readonly autoCloseOnMaxSelection = input<boolean, BooleanInput>(this.#config.autoCloseOnMaxSelection, {
		transform: booleanAttribute,
	})

	/** Defines how the date should be displayed in the UI.  */
	readonly formatDates = input<(date: T[]) => string>(this.#config.formatDates)

	/** Defines how the date should be transformed before saving to model/form. */
	readonly transformDates = input<(date: T[]) => T[]>(this.#config.transformDates)

	protected readonly popoverState = signal<BrnOverlayState | null>(null)

	protected readonly internalDisabled = linkedSignal(this.disabled)

	/** @internal The disabled state as a readonly signal */
	readonly disabledState = this.internalDisabled.asReadonly()

	readonly formattedDate = computed(() => {
		const dates = this.mutableDate()

		return dates ? this.formatDates()(dates) : undefined
	})

	readonly dateChange = output<T[]>()

	readonly labelableId = computed(() => this.trigger()?.triggerId())

	readonly hasDate = computed(() => !!this.mutableDate()?.length)

	/** @internal The current raw value, used by inputs to reformat on focus. */
	readonly value = computed(() => this.mutableDate() ?? null)

	protected onChange?: ChangeFn<T[]>
	protected onTouched?: TouchFn

	protected onStateChange(state: BrnOverlayState): void {
		this.popoverState.set(state)
		if (state === 'closed') this.onTouched?.()
	}

	protected handleChange(value: T[] | undefined): void {
		if (value === undefined) return

		if (this.internalDisabled()) return

		const transformedDate = value !== undefined ? this.transformDates()(value) : value

		this.mutableDate.set(transformedDate)
		this.onChange?.(transformedDate)
		this.dateChange.emit(transformedDate)

		if (this.autoCloseOnMaxSelection() && this.mutableDate()?.length === this.maxSelection()) {
			this.popoverState.set('closed')
		}
	}

	/**
	 * Commit dates to the picker. Updates the internal model, notifies form
	 * controls, and emits `dateChange`. Intended to be called from a text input
	 * that parses user-entered values. Pass `null` to clear the selection.
	 */
	updateDate(value: T[] | null): void {
		if (this.internalDisabled()) return

		const transformedDate = value ? this.transformDates()(value) : undefined

		this.mutableDate.set(transformedDate)
		this.onChange?.(transformedDate ?? [])
		this.dateChange.emit(transformedDate ?? [])
	}

	touched(): void {
		this.onTouched?.()
	}

	/** CONTROL VALUE ACCESSOR */
	writeValue(value: T[] | null): void {
		this.mutableDate.set(value ? this.transformDates()(value) : undefined)
	}

	registerOnChange(fn: ChangeFn<T[]>): void {
		this.onChange = fn
	}

	registerOnTouched(fn: TouchFn): void {
		this.onTouched = fn
	}

	setDisabledState(isDisabled: boolean): void {
		this.internalDisabled.set(isDisabled)
	}

	open(): void {
		this.popoverState.set('open')
	}

	close(): void {
		this.popoverState.set('closed')
	}

	reset(): void {
		this.mutableDate.set(undefined)
		this.onChange?.([])
		this.dateChange.emit([])
	}
}
