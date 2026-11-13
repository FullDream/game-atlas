import { sort, unique } from 'remeda'
import type { BooleanInput, NumberInput } from '@angular/cdk/coercion'
import { Component, booleanAttribute, computed, input, model, numberAttribute } from '@angular/core'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmPagination } from './hlm-pagination'
import { HlmPaginationContent } from './hlm-pagination-content'
import { HlmPaginationEllipsis } from './hlm-pagination-ellipsis'
import { HlmPaginationItem } from './hlm-pagination-item'
import { HlmPaginationLink } from './hlm-pagination-link'
import { HlmPaginationNext } from './hlm-pagination-next'
import { HlmPaginationPrevious } from './hlm-pagination-previous'
import { injectHlmPaginationConfig } from './hlm-pagination.token'

@Component({
	selector: 'hlm-numbered-pagination',
	exportAs: 'hlmNumberedPagination',
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
	templateUrl: './hlm-numbered-pagination.html',
})
export class HlmNumberedPagination {
	readonly #config = injectHlmPaginationConfig()

	/**
	 * The current (active) page.
	 */
	readonly currentPage = model.required<number>()

	/**
	 * The number of items per paginated page.
	 */
	readonly itemsPerPage = model.required<number>()

	/**
	 * The total number of items in the collection. Only useful when
	 * doing server-side paging, where the collection size is limited
	 * to a single page returned by the server API.
	 */
	readonly totalItems = input.required<number, NumberInput>({
		transform: numberAttribute,
	})

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
		sort(unique([...this.pageSizes(), this.itemsPerPage()]), (a, b) => a - b),
	)

	protected readonly isFirstPageActive = computed(() => this.currentPage() === 1)
	protected readonly isLastPageActive = computed(() => this.currentPage() === this.lastPageNumber())

	protected readonly lastPageNumber = computed(() => Math.max(1, Math.ceil(this.totalItems() / this.itemsPerPage())))

	protected readonly pages = computed(() =>
		createPageArray(
			outOfBoundCorrection(this.totalItems(), this.itemsPerPage(), this.currentPage()),
			this.itemsPerPage(),
			this.totalItems(),
			this.maxSize(),
		),
	)

	protected goToPrevious(): void {
		this.currentPage.set(this.currentPage() - 1)
	}

	protected goToNext(): void {
		this.currentPage.set(this.currentPage() + 1)
	}

	protected goToFirst(): void {
		this.currentPage.set(1)
	}

	protected goToLast(): void {
		this.currentPage.set(this.lastPageNumber())
	}
}

type Page = number | '...'

/**
 * Checks that the instance.currentPage property is within bounds for the current page range.
 * If not, return a correct value for currentPage, or the current value if OK.
 *
 * Copied from 'ngx-pagination' package
 */
export function outOfBoundCorrection(totalItems: number, itemsPerPage: number, currentPage: number): number {
	const totalPages = Math.max(1, Math.ceil(totalItems / itemsPerPage))

	return Math.min(Math.max(currentPage, 1), totalPages)
}

/**
 * Returns an array of Page objects to use in the pagination controls.
 *
 * Copied from 'ngx-pagination' package
 */
export function createPageArray(
	currentPage: number,
	itemsPerPage: number,
	totalItems: number,
	paginationRange: number,
): Page[] {
	// paginationRange could be a string if passed from attribute, so cast to number.
	paginationRange = +paginationRange
	const pages: Page[] = []

	// Return 1 as default page number
	// Make sense to show 1 instead of empty when there are no items
	const totalPages = Math.max(Math.ceil(totalItems / itemsPerPage), 1)
	const halfWay = Math.ceil(paginationRange / 2)

	const isStart = currentPage <= halfWay
	const isEnd = totalPages - halfWay < currentPage
	const isMiddle = !isStart && !isEnd

	const ellipsesNeeded = paginationRange < totalPages
	let i = 1

	while (i <= totalPages && i <= paginationRange) {
		let label: number | '...'
		const pageNumber = calculatePageNumber(i, currentPage, paginationRange, totalPages)
		const openingEllipsesNeeded = i === 2 && (isMiddle || isEnd)
		const closingEllipsesNeeded = i === paginationRange - 1 && (isMiddle || isStart)

		if (ellipsesNeeded && (openingEllipsesNeeded || closingEllipsesNeeded)) {
			label = '...'
		} else {
			label = pageNumber
		}

		pages.push(label)
		i++
	}

	return pages
}

/**
 * Given the position in the sequence of pagination links [i],
 * figure out what page number corresponds to that position.
 *
 * Copied from 'ngx-pagination' package
 */
function calculatePageNumber(i: number, currentPage: number, paginationRange: number, totalPages: number): number {
	const halfWay = Math.ceil(paginationRange / 2)

	if (i === paginationRange) {
		return totalPages
	}

	if (i === 1) {
		return i
	}

	if (paginationRange < totalPages) {
		if (totalPages - halfWay < currentPage) {
			return totalPages - paginationRange + i
		}

		if (halfWay < currentPage) {
			return currentPage - halfWay + i
		}

		return i
	}

	return i
}
