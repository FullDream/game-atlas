import { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matKeyboardArrowDownRound } from '@ng-icons/material-symbols/round'
import {
	type BrnDatePickerTriggerBase,
	injectBrnDatePicker,
	provideBrnDatePickerTrigger,
} from '@spartan-ng/brain/date-picker'
import { BrnFieldControl, BrnFieldControlDescribedBy } from '@spartan-ng/brain/field'
import { ButtonVariants, HlmButtonImports } from '@spartan-ng/helm/button'
import { HlmPopoverTrigger } from '@spartan-ng/helm/popover'
import { hlm } from '@spartan-ng/helm/utils'
import { ClassValue } from 'clsx'

@Component({
	selector: 'hlm-date-picker-trigger',
	exportAs: 'hlmDatePickerTrigger',
	imports: [HlmButtonImports, HlmPopoverTrigger, NgIcon, BrnFieldControlDescribedBy],
	templateUrl: './hlm-date-picker-trigger.html',
	providers: [provideIcons({ matKeyboardArrowDownRound }), provideBrnDatePickerTrigger(HlmDatePickerTrigger)],
	host: { 'data-slot': 'date-picker-trigger' },
})
export class HlmDatePickerTrigger implements BrnDatePickerTriggerBase {
	protected static nextId = 0

	readonly #fieldControl = inject(BrnFieldControl, { optional: true })
	readonly #datePicker = injectBrnDatePicker()

	readonly #invalid = this.#fieldControl?.invalid
	protected readonly spartanInvalid = computed(() => this.forceInvalid() || this.#fieldControl?.spartanInvalid())
	protected readonly dirty = this.#fieldControl?.dirty
	protected readonly touched = this.#fieldControl?.touched

	protected readonly ariaInvalid = computed(() => (this.#invalid?.() ? 'true' : null))

	readonly userClass = input<ClassValue>('', { alias: 'class' })
	protected readonly computedClass = computed(() =>
		hlm('data-placeholder:text-muted-foreground justify-between', this.userClass()),
	)

	protected readonly isPlaceholder = computed(() => !this.#datePicker.hasDate())

	/** The id of the button that opens the date picker. */
	readonly buttonId = input<string>(`hlm-date-picker-${++HlmDatePickerTrigger.nextId}`)

	/** @internal The id of the button that opens the date picker, used for labeling. */
	readonly triggerId = this.buttonId

	/** Forces the invalid state visually, regardless of form control state. */
	readonly forceInvalid = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	readonly variant = input<ButtonVariants['variant']>('outline')

	readonly showTrigger = input<boolean, BooleanInput>(true, { transform: booleanAttribute })

	protected readonly popover = this.#datePicker.popover
	protected readonly disabled = this.#datePicker.disabledState
	protected readonly formattedDate = this.#datePicker.formattedDate
}
