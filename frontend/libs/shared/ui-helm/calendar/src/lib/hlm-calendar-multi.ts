import { NgTemplateOutlet } from '@angular/common'
import { Component, computed, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matChevronLeftRound, matChevronRightRound } from '@ng-icons/material-symbols/round'
import { BrnCalendarImports, BrnCalendarMulti, injectBrnCalendarI18n } from '@spartan-ng/brain/calendar'
import { injectDateAdapter } from '@spartan-ng/brain/date-time'
import { buttonVariants, HlmButtonImports } from '@spartan-ng/helm/button'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { classes, hlm } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-calendar-multi',
	exportAs: 'hlmCalendarMulti',
	imports: [BrnCalendarImports, NgIcon, NgTemplateOutlet, HlmSelectImports, HlmButtonImports],
	templateUrl: './hlm-calendar-multi.html',
	viewProviders: [provideIcons({ matChevronLeftRound, matChevronRightRound })],
	hostDirectives: [
		{
			directive: BrnCalendarMulti,
			inputs: [
				'min',
				'max',
				'minSelection',
				'maxSelection',
				'disabled',
				'date',
				'dateDisabled',
				'weekStartsOn',
				'highlightDays',
				'defaultFocusedDate',
			],
			outputs: ['dateChange'],
		},
	],
	host: { 'data-slot': 'calendar' },
})
export class HlmCalendarMulti<T> {
	/** Show dropdowns to navigate between months or years. */
	readonly captionLayout = input<'dropdown' | 'label' | 'dropdown-months' | 'dropdown-years'>('label')

	/** Access the calendar i18n */
	protected readonly i18n = injectBrnCalendarI18n()

	/** Access the date time adapter */
	protected readonly dateAdapter = injectDateAdapter<T>()

	/** Access the calendar directive */
	readonly #calendar = inject(BrnCalendarMulti)

	/** Get the heading for the current month and year */
	protected readonly heading = computed(() => {
		const config = this.i18n.config()
		const date = this.#calendar.focusedDate()

		return {
			header: config.formatHeader(this.dateAdapter.getMonth(date), this.dateAdapter.getYear(date)),
			month: config.formatMonth(this.dateAdapter.getMonth(date)),
			year: config.formatYear(this.dateAdapter.getYear(date)),
		}
	})

	protected readonly btnClass = hlm(
		buttonVariants({ variant: 'ghost', size: 'icon' }),
		'data-[today=true]:bg-muted group-data-[focused=true]/day:border-ring group-data-[focused=true]/day:ring-ring/50 data-[range-end=true]:bg-primary data-[range-end=true]:text-primary-foreground data-[range-middle=true]:bg-muted data-[range-middle=true]:text-foreground data-[range-start=true]:bg-primary data-[range-start=true]:text-primary-foreground data-[selected-single=true]:bg-primary data-[selected-single=true]:text-primary-foreground dark:hover:bg-muted/50 dark:hover:text-foreground gap-1 font-normal [&>span]:text-xs relative isolate z-10 flex aspect-square size-auto w-full min-w-(--cell-size) flex-col border-0 leading-none group-data-[focused=true]/day:relative group-data-[focused=true]/day:z-10 group-data-[focused=true]/day:ring-3 data-[range-end=true]:rounded-(--cell-radius) data-[range-end=true]:rounded-e-(--cell-radius) data-[range-middle=true]:rounded-none data-[range-start=true]:rounded-(--cell-radius) data-[range-start=true]:rounded-s-(--cell-radius) [&>span]:opacity-70',
		'data-[outside=true]:opacity-50',
		"data-[highlighted]:before:content-['']",
		'data-[highlighted]:before:absolute',
		'data-[highlighted]:before:bottom-1',
		'data-[highlighted]:before:start-1/2',
		'data-[highlighted]:before:h-1',
		'data-[highlighted]:before:w-1',
		'data-[highlighted]:before:-translate-x-1/2',
		'data-[highlighted]:before:rounded-full',
		'data-[highlighted]:before:bg-destructive',
	)

	protected readonly selectClass = 'gap-0 px-1.5 py-2 [&>ng-icon]:ms-1'

	constructor() {
		classes(
			() =>
				'p-3 [--cell-size:--spacing(8)] group/calendar bg-background block [--cell-radius:var(--radius-4xl)] in-data-[slot=card-content]:bg-transparent in-data-[slot=popover-content]:bg-transparent',
		)
	}
}
