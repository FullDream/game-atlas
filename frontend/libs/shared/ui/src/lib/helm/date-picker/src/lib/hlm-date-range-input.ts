import { Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCalendarMonthRound, matCloseRound } from '@ng-icons/material-symbols/round'
import { BrnDateInput, type BrnDatePickerTriggerBase, provideBrnDatePickerTrigger } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl } from '@spartan-ng/brain/field'
import { HlmInputGroup, HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { injectHlmDateRangePickerConfig } from './hlm-date-range-picker.token'

@Component({
	selector: 'hlm-date-range-input',
	exportAs: 'hlmDateRangeInput',
	imports: [HlmInputGroupImports, NgIcon],
	templateUrl: './hlm-date-range-input.html',
	providers: [provideIcons({ matCalendarMonthRound, matCloseRound }), provideBrnDatePickerTrigger(HlmDateRangeInput)],
	hostDirectives: [HlmInputGroup],
})
export class HlmDateRangeInput<T> extends BrnDateInput<[T, T]> implements BrnDatePickerTriggerBase {
	readonly #config = injectHlmDateRangePickerConfig<T>()
	readonly #fieldControl = inject(BrnFieldControl, { optional: true })

	readonly #invalid = this.#fieldControl?.invalid
	protected readonly spartanInvalid = computed(() => this.forceInvalid() || this.#fieldControl?.spartanInvalid())
	protected readonly dirty = this.#fieldControl?.dirty
	protected readonly touched = this.#fieldControl?.touched

	protected readonly ariaInvalid = computed(() => (this.#invalid?.() ? 'true' : null))

	/**
	 * Parses input text into a date range. Return `null` for invalid
	 * input - the picker's range is cleared while the text is preserved so
	 * the user can fix it.
	 *
	 * Defaults to `parseDate` from `HlmDateRangePickerConfig`.
	 */
	readonly parseDate = input<(value: string) => [T, T] | null>(this.#config.parseDate)

	/**
	 * Formats the current range into the input/edit format shown while the
	 * input is focused. On blur the picker's display format is restored.
	 *
	 * Defaults to `formatInputDates` from `HlmDateRangePickerConfig`.
	 */
	readonly formatInputDates = input<(dates: [T | null, T | null]) => string>(this.#config.formatInputDates)

	protected override parseValue(value: string): [T, T] | null {
		return this.parseDate()(value)
	}

	protected override formatInputValue(value: [T, T]): string {
		return this.formatInputDates()(value)
	}
}
