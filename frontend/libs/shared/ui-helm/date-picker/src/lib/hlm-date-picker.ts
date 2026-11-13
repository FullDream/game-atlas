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
	viewChild,
} from '@angular/core'
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms'
import { type BrnDatePickerBase, BrnDatePickerTriggerToken, provideBrnDatePicker } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl, provideBrnLabelable } from '@spartan-ng/brain/field'
import type { ChangeFn, TouchFn } from '@spartan-ng/brain/forms'
import type { BrnOverlayState } from '@spartan-ng/brain/overlay'
import { BrnPopover, type BrnPopoverAlign } from '@spartan-ng/brain/popover'
import { HlmCalendar } from '@spartan-ng/helm/calendar'
import { HlmPopoverImports } from '@spartan-ng/helm/popover'
import { injectHlmDatePickerConfig } from './hlm-date-picker.token'

export const HLM_DATE_PICKER_VALUE_ACCESSOR = {
	provide: NG_VALUE_ACCESSOR,
	useExisting: forwardRef(() => HlmDatePicker),
	multi: true,
}

@Component({
	selector: 'hlm-date-picker',
	exportAs: 'hlmDatePicker',
	imports: [HlmPopoverImports, HlmCalendar],
	templateUrl: './hlm-date-picker.html',
	providers: [
		HLM_DATE_PICKER_VALUE_ACCESSOR,
		provideBrnDatePicker(HlmDatePicker),
		provideBrnLabelable(HlmDatePicker),
	],
	hostDirectives: [BrnFieldControl],
	host: { class: 'block' },
})
export class HlmDatePicker<T> implements BrnDatePickerBase<T>, ControlValueAccessor {
	readonly #config = injectHlmDatePickerConfig<T>()

	readonly popover = viewChild.required(BrnPopover)

	protected readonly trigger = contentChild(BrnDatePickerTriggerToken)

	readonly align = input<BrnPopoverAlign>('center')

	/** Show dropdowns to navigate between months or years. */
	readonly captionLayout = input<'dropdown' | 'label' | 'dropdown-months' | 'dropdown-years'>('label')

	/** The minimum date that can be selected. */
	readonly minDate = input<T>()

	/** The maximum date that can be selected. */
	readonly maxDate = input<T>()

	/** Determine if the date picker is disabled. */
	readonly disabled = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})

	/** The selected value. */
	readonly date = input<T>()

	/** The date the calendar focuses on first open when no date is selected. */
	readonly defaultFocusedDate = input<T>()

	protected readonly mutableDate = linkedSignal(this.date)

	/** If true, the date picker will close when a date is selected. */
	readonly autoCloseOnSelect = input<boolean, BooleanInput>(this.#config.autoCloseOnSelect, {
		transform: booleanAttribute,
	})

	/** Defines how the date should be displayed in the UI.  */
	readonly formatDate = input<(date: T) => string>(this.#config.formatDate)

	/** Defines how the date should be transformed before saving to model/form. */
	readonly transformDate = input<(date: T) => T>(this.#config.transformDate)

	protected readonly popoverState = signal<BrnOverlayState | null>(null)

	protected readonly internalDisabled = linkedSignal(this.disabled)

	/** @internal The disabled state as a readonly signal */
	readonly disabledState = this.internalDisabled.asReadonly()

	readonly formattedDate = computed(() => {
		const date = this.mutableDate()

		return date ? this.formatDate()(date) : undefined
	})

	readonly dateChange = output<T | null>()

	readonly labelableId = computed(() => this.trigger()?.triggerId())

	readonly hasDate = computed(() => !!this.mutableDate())

	/** @internal The current raw value, used by inputs to reformat on focus. */
	readonly value = computed(() => this.mutableDate() ?? null)

	protected onChange?: ChangeFn<T | null>
	protected onTouched?: TouchFn

	protected onStateChange(state: BrnOverlayState): void {
		this.popoverState.set(state)
		if (state === 'closed') this.onTouched?.()
	}

	protected handleChange(value: T | undefined): void {
		if (this.internalDisabled()) return

		this.updateDate(value ?? null)

		if (this.autoCloseOnSelect()) {
			this.popoverState.set('closed')
		}
	}

	/**
	 * Commit a date to the picker. Updates the internal model, notifies form
	 * controls, and emits `dateChange`. Unlike `handleChange`, this does not
	 * close the popover - it's intended to be called from a text input that
	 * is parsing user-entered values while typing.
	 */
	updateDate(value: T | null): void {
		if (this.internalDisabled()) return

		const transformedDate = value != null ? this.transformDate()(value) : undefined

		this.mutableDate.set(transformedDate)
		this.onChange?.(transformedDate ?? null)
		this.dateChange.emit(transformedDate ?? null)
	}

	/** CONTROL VALUE ACCESSOR */
	writeValue(value: T | null): void {
		this.mutableDate.set(value ? this.transformDate()(value) : undefined)
	}

	registerOnChange(fn: ChangeFn<T | null>): void {
		this.onChange = fn
	}

	registerOnTouched(fn: TouchFn): void {
		this.onTouched = fn
	}

	touched(): void {
		this.onTouched?.()
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
		this.onChange?.(null)
		this.dateChange.emit(null)
	}
}
