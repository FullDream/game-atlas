import { CdkDropList, type CdkDrag, type CdkDragDrop } from '@angular/cdk/drag-drop'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { TooltipOnOverflow } from '../tooltip/tooltip-on-overflow'
import {
	afterNextRender,
	afterRenderEffect,
	Component,
	computed,
	DestroyRef,
	ElementRef,
	inject,
	input,
	signal,
	viewChild,
} from '@angular/core'
import { RouterLink } from '@angular/router'
import { HlmBreadcrumbImports } from '@spartan-ng/helm/breadcrumb'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { dropLast, takeLast } from 'remeda'
import type { WorkspaceBreadcrumb } from './workspace-navigation'

type BreadcrumbMetrics = {
	width: number
	labels: number[]
	workspace: number
	separator: number
	ellipsis: number
	gap: number
}

@Component({
	selector: 'ui-workspace-breadcrumbs',
	imports: [CdkDropList, HlmTooltip, TooltipOnOverflow, RouterLink, HlmBreadcrumbImports, HlmDropdownMenuImports],
	templateUrl: './workspace-breadcrumbs.html',
	host: { class: 'relative block min-w-0' },
})
export class WorkspaceBreadcrumbs {
	readonly items = input.required<WorkspaceBreadcrumb[]>()

	readonly #element = inject(ElementRef<HTMLElement>)
	readonly #destroy = inject(DestroyRef)
	readonly #metrics = signal<BreadcrumbMetrics | null>(null)

	protected readonly measurement = viewChild<ElementRef<HTMLElement>>('measurement')

	protected readonly layout = computed(() => {
		const metrics = this.#metrics()
		const length = this.items().length

		if (!metrics || metrics.labels.length !== length) return { count: length, showRoot: false }

		for (let count = length; count > 1; count--) {
			const collapsed = count < length

			for (const showRoot of collapsed ? [true, false] : [false]) {
				const labels = showRoot
					? [metrics.labels[0], ...takeLast(metrics.labels, count - 1)]
					: takeLast(metrics.labels, count)
				const parts = count + Number(collapsed) + Number(metrics.workspace > 0)
				const width =
					labels.reduce((total, label) => total + label, 0) +
					metrics.workspace +
					(collapsed ? metrics.ellipsis : 0) +
					(parts - 1) * (metrics.separator + metrics.gap * 2)

				if (width <= metrics.width) return { count, showRoot }
			}
		}

		return { count: Math.min(1, length), showRoot: false }
	})

	protected readonly first = computed(() => (this.layout().showRoot ? this.items()[0] : null))

	protected readonly tail = computed(() => {
		const count = this.layout().count - (this.first() ? 1 : 0)

		return takeLast(this.items(), count)
	})

	protected readonly hidden = computed(() => {
		const items = this.first() ? this.items().slice(1) : this.items()

		return dropLast(items, this.tail().length)
	})

	constructor() {
		afterRenderEffect(() => {
			this.items()
			this.#measure()
		})

		afterNextRender(() => {
			if (typeof ResizeObserver === 'undefined') {
				return
			}

			const observer = new ResizeObserver(() => this.#measure())

			observer.observe(this.#element.nativeElement)
			const measurement = this.measurement()?.nativeElement

			if (measurement) observer.observe(measurement)

			this.#destroy.onDestroy(() => observer.disconnect())
		})
	}

	protected readonly acceptsDrop = (drag: CdkDrag<string>, list: CdkDropList<WorkspaceBreadcrumb>): boolean =>
		!list.disabled && (list.data.drop?.accepts(drag.data) ?? false)

	protected drop(event: CdkDragDrop<WorkspaceBreadcrumb, unknown, string>): void {
		const target = event.container.data.drop

		if (event.isPointerOverContainer && target?.accepts(event.item.data)) {
			target.perform(event.item.data)
		}
	}

	#measure(): void {
		const measurement = this.measurement()?.nativeElement

		if (!measurement) return

		const width = this.#element.nativeElement.getBoundingClientRect().width

		if (!width) return

		const labels = Array.from(
			measurement.querySelectorAll<HTMLElement>('[data-label]'),
			element => element.getBoundingClientRect().width,
		)

		this.#metrics.set({
			width,
			labels,
			workspace: measurement.querySelector('[data-workspace]')?.getBoundingClientRect().width ?? 0,
			separator: measurement.querySelector('[hlmBreadcrumbSeparator]')?.getBoundingClientRect().width ?? 0,
			ellipsis: measurement.querySelector('hlm-breadcrumb-ellipsis')?.getBoundingClientRect().width ?? 0,
			gap: Number.parseFloat(getComputedStyle(measurement).columnGap) || 0,
		})
	}
}
