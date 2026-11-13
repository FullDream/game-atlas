import { Component, computed, inject } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matChevronLeftRound, matChevronRightRound } from '@ng-icons/material-symbols/round'
import { BrnCalendarImports, BrnMonthYearCalendar, injectBrnCalendarI18n } from '@spartan-ng/brain/calendar'
import { injectDateAdapter } from '@spartan-ng/brain/date-time'
import { buttonVariants, HlmButtonImports } from '@spartan-ng/helm/button'
import { classes, hlm } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-month-year-calendar',
	exportAs: 'hlmMonthYearCalendar',
	imports: [BrnCalendarImports, NgIcon, HlmButtonImports],
	templateUrl: './hlm-month-year-calendar.html',
	viewProviders: [provideIcons({ matChevronLeftRound, matChevronRightRound })],
	hostDirectives: [
		{
			directive: BrnMonthYearCalendar,
			inputs: ['min', 'max', 'disabled', 'date', 'defaultFocusedDate', 'view'],
			outputs: ['dateChange'],
		},
	],
	host: { 'data-slot': 'month-year-calendar' },
})
export class HlmMonthYearCalendar<T> {
	/** Access the calendar i18n */
	protected readonly i18n = injectBrnCalendarI18n()

	/** Access the date adapter */
	protected readonly dateAdapter = injectDateAdapter<T>()

	/** Access the picker directive */
	protected readonly picker = inject(BrnMonthYearCalendar<T>)

	/** The heading for the current view. */
	protected readonly heading = computed(() => {
		const config = this.i18n.config()

		if (this.picker.view() === 'month') {
			return config.formatYear(this.dateAdapter.getYear(this.picker.focusedDate()))
		}

		const { start, end } = this.picker.yearRange()

		return `${config.formatYear(start)} – ${config.formatYear(end)}`
	})

	protected readonly btnClass = hlm(
		buttonVariants({ variant: 'ghost' }),
		'data-[today=true]:bg-muted',
		'data-[selected=true]:bg-primary data-[selected=true]:text-primary-foreground data-[selected=true]:hover:bg-primary data-[selected=true]:hover:text-primary-foreground',
		'data-[focused=true]:border-ring data-[focused=true]:ring-ring/50 data-[focused=true]:ring-3',
		'aria-disabled:pointer-events-none aria-disabled:opacity-50',
		'h-(--cell-size)',
	)

	constructor() {
		classes(
			() =>
				'p-3 [--cell-size:--spacing(8)] group/calendar bg-background block [--cell-radius:var(--radius-4xl)] in-data-[slot=card-content]:bg-transparent in-data-[slot=popover-content]:bg-transparent',
		)
	}
}
