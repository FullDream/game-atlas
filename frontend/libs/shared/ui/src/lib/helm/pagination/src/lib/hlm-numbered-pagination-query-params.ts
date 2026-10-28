import { sort, unique } from 'remeda'
import type { BooleanInput, NumberInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, input, model, numberAttribute } from '@angular/core'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { classes } from '@spartan-ng/helm/utils'
import { createPageArray, outOfBoundCorrection } from './hlm-numbered-pagination'
import { HlmPagination } from './hlm-pagination'
import { HlmPaginationContent } from './hlm-pagination-content'
import { HlmPaginationEllipsis } from './hlm-pagination-ellipsis'
import { HlmPaginationItem } from './hlm-pagination-item'
import { HlmPaginationLink } from './hlm-pagination-link'
import { HlmPaginationNext } from './hlm-pagination-next'
import { HlmPaginationPrevious } from './hlm-pagination-previous'
import { injectHlmPaginationConfig } from './hlm-pagination.token'

@Component({
	selector: 'hlm-numbered-pagination-query-params',
	exportAs: 'hlmNumberedPaginationQueryParams',
	imports: [
		HlmPagination,
		HlmPaginationContent,
		HlmPaginationItem,
		HlmPaginationPrevious,
		HlmPaginationNext,
		HlmPaginationLink,
		HlmPaginationEllipsis,
		HlmSelectImports,
	],
	templateUrl: './hlm-numbered-pagination-query-params.html',
})
export class HlmNumberedPaginationQueryParams {
	readonly #config = injectHlmPaginationConfig()

	/**
	 * The zero-based offset of the first item.
	 */
	readonly offset = model.required<number>()

	/**
	 * The number of items per paginated page.
	 */
	readonly limit = model.required<number>()

	/**
	 * The total number of items in the collection. Only useful when
	 * doing server-side paging, where the collection size is limited
	 * to a single page returned by the server API.
	 */
	readonly totalItems = input.required<number, NumberInput>({
		transform: numberAttribute,
	})

	/**
	 * The URL path to use for the pagination links.
	 * Defaults to '.' (current path).
	 */
	readonly link = input<string>('.')

	/**
	 * The number of page links to show.
	 */
	readonly maxSize = input<number, NumberInput>(7, {
		transform: numberAttribute,
	})

	/**
	 * Show the first and last page buttons.
	 */
	readonly showEdges = input<boolean, BooleanInput>(true, {
		transform: booleanAttribute,
	})

	/**
	 * The page sizes to show.
	 * Defaults to [10, 20, 50, 100]
	 */
	readonly pageSizes = input<number[]>([10, 20, 50, 100])

	/**
	 * The label for total items or pages to show
	 * Defaults to config's totalItemsLabel
	 */
	readonly totalItemLabel = input<string>(this.#config.totalItemLabel)

	readonly totalItemsLabel = input<string>(this.#config.totalItemsLabel)

	/**
	 * The label for pages to show
	 * Defaults to config's pagesLabel
	 */
	readonly pageLabel = input<string>(this.#config.pageLabel)

	readonly pagesLabel = input<string>(this.#config.pagesLabel)

	/**
	 * The label to show on the next button
	 * Defaults to config's nextLabel
	 */
	readonly nextLabel = input<string>(this.#config.nextLabel)

	/**
	 * The aria-label to show on the next button
	 * Defaults to config's nextAriaLabel
	 */
	readonly nextAriaLabel = input<string>(this.#config.nextAriaLabel)

	/**
	 * The label to show on the previous button
	 * Defaults to config's previousLabel
	 */
	readonly previousLabel = input<string>(this.#config.previousLabel)

	/**
	 * The aria-label to show on the previous button
	 * Defaults to config's previousAriaLabel
	 */
	readonly previousAriaLabel = input<string>(this.#config.previousAriaLabel)

	protected readonly pageSizesWithCurrent = computed(() =>
		sort(unique([...this.pageSizes(), this.limit()]), (a, b) => a - b),
	)

	protected readonly lastPageNumber = computed(() => Math.max(1, Math.ceil(this.totalItems() / this.limit())))

	protected readonly pages = computed(() =>
		createPageArray(
			outOfBoundCorrection(this.totalItems(), this.limit(), this.currentPage()),
			this.limit(),
			this.totalItems(),
			this.maxSize(),
		),
	)

	protected readonly currentPage = computed(() =>
		Math.min(Math.floor(this.offset() / this.limit()) + 1, this.lastPageNumber()),
	)

	protected readonly isFirstPageActive = computed(() => this.currentPage() === 1)
	protected readonly isLastPageActive = computed(() => this.currentPage() === this.lastPageNumber())

	constructor() {
		classes(() => 'gap-x-3 gap-y-2 px-4 py-2 sm:flex-nowrap flex flex-wrap items-center justify-between')
	}
}
