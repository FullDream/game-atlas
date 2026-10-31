import { Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCalendarMonthRound, matCloseRound } from '@ng-icons/material-symbols/round'
import { BrnDateInput, type BrnDatePickerTriggerBase, provideBrnDatePickerTrigger } from '@spartan-ng/brain/date-picker'
import { BrnFieldControl } from '@spartan-ng/brain/field'
import { HlmInputGroup, HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { injectHlmDatePickerConfig } from './hlm-date-picker.token'

@Component({
	selector: 'hlm-date-picker-input',
	exportAs: 'hlmDatePickerInput',
	imports: [HlmInputGroupImports, NgIcon],
	templateUrl: './hlm-date-picker-input.html',
	providers: [
		provideIcons({ matCalendarMonthRound, matCloseRound }),
		provideBrnDatePickerTrigger(HlmDatePickerInput),
	],
	hostDirectives: [HlmInputGroup],
})
export class HlmDatePickerInput<T> extends BrnDateInput<T> implements BrnDatePickerTriggerBase {
	readonly #config = injectHlmDatePickerConfig<T>()
	readonly #fieldControl = inject(BrnFieldControl, { optional: true })

	readonly #invalid = this.#fieldControl?.invalid
	protected readonly spartanInvalid = computed(() => this.forceInvalid() || this.#fieldControl?.spartanInvalid())
	protected readonly dirty = this.#fieldControl?.dirty
	protected readonly touched = this.#fieldControl?.touched

	protected readonly ariaInvalid = computed(() => (this.#invalid?.() ? 'true' : null))

	/**
	 * Parses input text into a date value. Return `null` for invalid
	 * input - the picker's date is cleared while the text is preserved so
	 * the user can fix it.
	 *
	 * Defaults to `parseDate` from `HlmDatePickerConfig`.
	 */
	readonly parseDate = input<(value: string) => T | null>(this.#config.parseDate)

	/**
	 * Formats the current date into the input/edit format shown while the
	 * input is focused. On blur the picker's display format is restored.
	 *
	 * Defaults to `formatInputDate` from `HlmDatePickerConfig`.
	 */
	readonly formatInputDate = input<(date: T) => string>(this.#config.formatInputDate)

	protected parseValue(value: string): T | null {
		return this.parseDate()(value)
	}

	protected formatInputValue(value: T): string {
		return this.formatInputDate()(value)
	}
}
