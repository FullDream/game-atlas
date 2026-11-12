import { booleanAttribute, Component, ElementRef, inject, input, model, signal, viewChild } from '@angular/core'
import { ColorPickerFormat, SimColorPickerImports } from '../sim/color-picker'
import { HlmPopover, HlmPopoverImports } from '@spartan-ng/helm/popover'
import { FormValueControl } from '@angular/forms/signals'
import { classes } from '@spartan-ng/helm/utils'
import { HlmInput } from '@spartan-ng/helm/input'
import { BrnPopover } from '@spartan-ng/brain/popover'

const PRESETS = [
	'#0A0A0A',
	'#171717',
	'#262626',
	'#404040',
	'#525252',
	'#737373',
	'#A3A3A3',
	'#D4D4D4',
	'#E5E5E5',
	'#F5F5F5',
	'#FAFAFA',
	'#FFFFFF',
] as const

@Component({
	selector: 'ui-color-picker',
	imports: [SimColorPickerImports, HlmPopoverImports],
	templateUrl: './color-picker.html',
	host: {
		class: 'cursor-pointer w-full text-s items-center inline-flex justify-start px-2.5 py-1 h-9 min-w-0',
		'aria-label': 'Open color picker',
		tabindex: '0',
		'data-slot': 'input-group-control',
		'(click)': 'trigger().nativeElement.click()',
		'(keydown.enter)': 'trigger().nativeElement.click()',
		'(keydown.space)': 'trigger().nativeElement.click()',
	},
	hostDirectives: [HlmInput, HlmPopover],
})
export class ColorPicker implements FormValueControl<string> {
	readonly group = input(false, { transform: booleanAttribute })
	readonly clearable = input(false, { transform: booleanAttribute })
	readonly value = model<string>('')
	readonly disabled = input(false)

	protected readonly presets = PRESETS

	protected readonly format = signal<ColorPickerFormat>('hex')
	protected readonly popover = inject(BrnPopover)
	protected readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger')

	constructor() {
		classes(() =>
			this.group()
				? `flex-1 rounded-none border-0 bg-transparent shadow-none ring-0 focus-visible:ring-0 data-[matches-spartan-invalid=true]:ring-0 dark:bg-transparent`
				: '',
		)
	}
}
