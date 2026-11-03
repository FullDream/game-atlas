import { Component, computed, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matKeyboardArrowDownRound } from '@ng-icons/material-symbols/round'
import { BrnAccordionImports } from '@spartan-ng/brain/accordion'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'

@Component({
	selector: 'hlm-accordion-trigger',
	exportAs: 'hlmAccordionTrigger',
	imports: [BrnAccordionImports, NgIcon],
	providers: [provideIcons({ matKeyboardArrowDownRound })],
	template: `
		<h3 brnAccordionHeader class="flex">
			<button brnAccordionTrigger data-slot="accordion-trigger" [class]="triggerClasses()">
				<ng-content />
				<ng-icon
					name="matKeyboardArrowDownRound"
					data-slot="accordion-trigger-icon"
					aria-hidden="true"
					class="pointer-events-none shrink-0 transition-transform duration-200 group-aria-expanded/accordion-trigger:rotate-180 motion-reduce:transition-none"
				/>
			</button>
		</h3>
	`,
})
export class HlmAccordionTrigger {
	public readonly triggerClass = input<ClassValue>('')

	protected readonly triggerClasses = computed(() =>
		hlm(
			'focus-visible:ring-ring/50 focus-visible:border-ring focus-visible:after:border-ring **:data-[slot=accordion-trigger-icon]:text-muted-foreground! rounded-md py-4 text-sm font-medium **:data-[slot=accordion-trigger-icon]:text-[length:--spacing(4)] group/accordion-trigger relative flex flex-1 items-start justify-between border border-transparent text-start transition-all outline-none hover:underline focus-visible:ring-3 aria-disabled:pointer-events-none aria-disabled:opacity-50 **:data-[slot=accordion-trigger-icon]:ms-auto',
			this.triggerClass(),
		),
	)
}
