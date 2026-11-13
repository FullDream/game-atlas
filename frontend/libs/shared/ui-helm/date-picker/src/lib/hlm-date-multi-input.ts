import { Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCalendarMonthRound, matCloseRound } from '@ng-icons/material-symbols/round'
import { BrnDateInput, type BrnDatePickerTriggerBase, provideBrnDatePickerTrigger } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl } from '@spartan-ng/brain/field'
import { HlmInputGroup, HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { injectHlmDatePickerMultiConfig } from './hlm-date-picker-multi.token'

@Component({
	selector: 'hlm-date-multi-input',
	exportAs: 'hlmDateMultiInput',
	imports: [HlmInputGroupImports, NgIcon],
	templateUrl: './hlm-date-multi-input.html',
	providers: [provideIcons({ matCalendarMonthRound, matCloseRound }), provideBrnDatePickerTrigger(HlmDateMultiInput)],
	hostDirectives: [HlmInputGroup],
})
export class HlmDateMultiInput<T> extends BrnDateInput<T[]> implements BrnDatePickerTriggerBase {
	readonly #config = injectHlmDatePickerMultiConfig<T>()
	readonly #fieldControl = inject(BrnFieldControl, { optional: true })

	readonly #invalid = this.#fieldControl?.invalid
	protected readonly spartanInvalid = computed(() => this.forceInvalid() || this.#fieldControl?.spartanInvalid())
	protected readonly dirty = this.#fieldControl?.dirty
	protected readonly touched = this.#fieldControl?.touched

	protected readonly ariaInvalid = computed(() => (this.#invalid?.() ? 'true' : null))
	/**
	 * Parses input text into dates. Return `null` for invalid input - the
	 * picker's dates are cleared while the text is preserved so the user can
	 * fix it.
	 *
	 * Defaults to `parseDate` from `HlmDatePickerMultiConfig`.
	 */
	readonly parseDate = input<(value: string) => T[] | null>(this.#config.parseDate)

	/**
	 * Formats the current dates into the input/edit format shown while the
	 * input is focused. On blur the picker's display format is restored.
	 *
	 * Defaults to `formatInputDates` from `HlmDatePickerMultiConfig`.
	 */
	readonly formatInputDates = input<(dates: T[]) => string>(this.#config.formatInputDates)

	protected override parseValue(value: string): T[] | null {
		return this.parseDate()(value)
	}

	protected override formatInputValue(value: T[]): string {
		return this.formatInputDates()(value)
	}
}
