import type { BooleanInput } from '@angular/cdk/coercion'
import {
	booleanAttribute,
	Component,
	computed,
	contentChild,
	forwardRef,
	input,
	linkedSignal,
	output,
	signal,
	untracked,
	viewChild,
} from '@angular/core'
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms'
import { type BrnDatePickerBase, BrnDatePickerTriggerToken, provideBrnDatePicker } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl, provideBrnLabelable } from '@spartan-ng/brain/field'
import type { ChangeFn, TouchFn } from '@spartan-ng/brain/forms'
import type { BrnOverlayState } from '@spartan-ng/brain/overlay'
import { BrnPopover, type BrnPopoverAlign } from '@spartan-ng/brain/popover'
import { HlmCalendarRange } from '@spartan-ng/helm/calendar'
import { HlmPopoverImports } from '@spartan-ng/helm/popover'
import { injectHlmDateRangePickerConfig } from './hlm-date-range-picker.token'

export const HLM_DATE_RANGE_PICKER_VALUE_ACCESSOR = {
	provide: NG_VALUE_ACCESSOR,
	useExisting: forwardRef(() => HlmDateRangePicker),
	multi: true,
}

@Component({
	selector: 'hlm-date-range-picker',
	exportAs: 'hlmDateRangePicker',
	imports: [HlmPopoverImports, HlmCalendarRange],
	templateUrl: './hlm-date-range-picker.html',
	providers: [
		HLM_DATE_RANGE_PICKER_VALUE_ACCESSOR,
		provideBrnDatePicker(HlmDateRangePicker),
		provideBrnLabelable(HlmDateRangePicker),
	],
	hostDirectives: [BrnFieldControl],
	host: { class: 'block' },
})
export class HlmDateRangePicker<T> implements BrnDatePickerBase<[T, T]>, ControlValueAccessor {
	readonly #config = injectHlmDateRangePickerConfig<T>()

	readonly popover = viewChild.required(BrnPopover)

	protected readonly trigger = contentChild(BrnDatePickerTriggerToken)

	readonly align = input<BrnPopoverAlign>('center')

	/** Show dropdowns to navigate between months or years. */
	readonly captionLayout = input<'dropdown' | 'label' | 'dropdown-months' | 'dropdown-years'>('label')

	/** The minimum date that can be selected.*/
	readonly minDate = input<T>()

	/** The maximum date that can be selected. */
	readonly maxDate = input<T>()

	/** Determine if the date picker is disabled. */
	readonly disabled = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})

	/** The selected value. */
	readonly date = input<[T, T]>()

	protected readonly mutableDate = linkedSignal(this.date)

	protected readonly start = linkedSignal(() => this.mutableDate()?.[0])
	protected readonly end = linkedSignal(() => this.mutableDate()?.[1])

	/** If true, the date picker will close when the end date is selected */
	readonly autoCloseOnEndSelection = input<boolean, BooleanInput>(this.#config.autoCloseOnEndSelection, {
		transform: booleanAttribute,
	})

	/** Defines how the date should be displayed in the UI.  */
	readonly formatDates = input<(dates: [T | null, T | null]) => string>(this.#config.formatDates)

	/** Defines how the date should be transformed before saving to model/form. */
	readonly transformDates = input<(date: [T, T]) => [T, T]>(this.#config.transformDates)

	protected readonly popoverState = signal<BrnOverlayState | null>(null)

	protected readonly internalDisabled = linkedSignal(this.disabled)

	/** @internal The disabled state as a readonly signal */
	readonly disabledState = this.internalDisabled.asReadonly()

	readonly formattedDate = computed(() => {
		const start = this.start()
		const end = this.end()

		return start || end ? this.formatDates()([start ?? null, end ?? null]) : undefined
	})

	readonly dateChange = output<[T, T] | null>()

	readonly labelableId = computed(() => this.trigger()?.triggerId())

	readonly hasDate = computed(() => !!this.start() || !!this.end())

	/** @internal The current raw value, used by inputs to reformat on focus. */
	readonly value = computed(() => this.mutableDate() ?? null)

	protected onChange?: ChangeFn<[T, T] | null>
	protected onTouched?: TouchFn

	protected onStateChange(state: BrnOverlayState): void {
		this.popoverState.set(state)
		if (state === 'closed') {
			this.onClose()
			this.onTouched?.()
		}
	}

	protected handleStartDayChange(value: T | undefined): void {
		this.start.set(value)
	}

	protected handleEndDateChange(value: T | undefined): void {
		this.end.set(value)
		if (this.internalDisabled()) return

		const start = this.start()

		if (start && value) {
			const transformedDates = this.transformDates()([start, value])

			this.mutableDate.set(transformedDates)
			this.dateChange.emit(transformedDates)
			this.onChange?.(transformedDates)

			if (this.autoCloseOnEndSelection()) {
				this.popoverState.set('closed')
			}
		}
	}

	/**
	 * Commit a range to the picker. Updates the internal model, notifies form
	 * controls, and emits `dateChange`. Intended to be called from a text input
	 * that parses user-entered values. Pass `null` to clear the range.
	 */
	updateDate(value: [T, T] | null): void {
		if (this.internalDisabled()) return

		if (!value) {
			this.mutableDate.set(undefined)
			this.start.set(undefined)
			this.end.set(undefined)
			this.onChange?.(null)
			this.dateChange.emit(null)

			return
		}

		const transformedDates = this.transformDates()(value)

		this.mutableDate.set(transformedDates)
		this.start.set(transformedDates[0])
		this.end.set(transformedDates[1])
		this.onChange?.(transformedDates)
		this.dateChange.emit(transformedDates)
	}

	touched(): void {
		this.onTouched?.()
	}

	/** CONTROL VALUE ACCESSOR */
	writeValue(value: [T, T] | null): void {
		untracked(() => {
			if (!value) {
				this.mutableDate.set(undefined)
			} else {
				this.mutableDate.set(this.transformDates()(value))
			}
		})
	}

	registerOnChange(fn: ChangeFn<[T, T] | null>): void {
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
		this.start.set(undefined)
		this.end.set(undefined)
		this.onChange?.(null)
		this.dateChange.emit(null)
	}

	protected onClose(): void {
		const dates = this.mutableDate()

		if (this.start() && !this.end() && dates) {
			this.start.set(dates[0])
			this.end.set(dates[1])
		}
	}
}
