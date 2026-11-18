import { Component, computed, effect, inject, signal } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matAddRound,
	matSearchRound,
	matGridViewRound,
	matViewListRound,
	matSortRound,
	matSportsEsportsRound,
	matCloudOffRound,
	matRefreshRound,
} from '@ng-icons/material-symbols/round'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { FormsModule } from '@angular/forms'
import { Router, RouterLink } from '@angular/router'
import {
	GAME_SORT_OPTIONS,
	GamesQueryOptions,
	getGameSortLabel,
	type ListGamesSortBy as GameSortBy,
	type ListGamesSortDirection as SortDirection,
	parseGameSort,
} from '@game-atlas/games/data-access'
import { Button, ErrorState, PageHeaderPortal } from '@game-atlas/shared/ui'
import { DEFAULT_PAGE_SIZE, PAGE_SIZES } from '@game-atlas/shared/util'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmNumberedPaginationQueryParams } from '@spartan-ng/helm/pagination'
import { injectQuery } from "@tanstack/angular-query"
import { linkedQueryParam } from 'ngxtension/linked-query-param'

import { GameCard } from './game-card/game-card'

@Component({
	selector: 'games-list',
	imports: [
		GameCard,
		NgIcon,
		HlmSelectImports,
		HlmToggleGroupImports,
		HlmEmptyImports,
		RouterLink,
		FormsModule,
		Button,
		ErrorState,
		PageHeaderPortal,
		HlmInput,
		HlmNumberedPaginationQueryParams,
	],
	templateUrl: './feature-list.html',
	styleUrl: './feature-list.css',
	providers: [
		provideIcons({
			matAddRound,
			matSearchRound,
			matGridViewRound,
			matViewListRound,
			matSortRound,
			matSportsEsportsRound,
			matCloudOffRound,
			matRefreshRound,
		}),
	],
	host: { class: '@container/games flex min-h-0 flex-1 flex-col' },
})
export class FeatureList {
	readonly #options = inject(GamesQueryOptions)
	readonly #router = inject(Router)

	protected readonly search = linkedQueryParam('search', {
		parse: value => value ?? '',
		stringify: value => value || null,
	})

	protected readonly offset = linkedQueryParam('offset', {
		parse: value => {
			const number = Number(value ?? 0)

			return Number.isSafeInteger(number) && number >= 0 && number <= 2147483647 ? number : 0
		},
		stringify: value => value || null,
	})

	protected readonly limit = linkedQueryParam('limit', {
		parse: value => {
			const number = Number(value ?? DEFAULT_PAGE_SIZE)

			return PAGE_SIZES.includes(number) ? number : DEFAULT_PAGE_SIZE
		},
		stringify: value => (value === DEFAULT_PAGE_SIZE ? null : value),
	})

	protected readonly sortBy = linkedQueryParam<GameSortBy>('sortBy', {
		parse: value => (value === 'createdAt' || value === 'updatedAt' ? value : 'name'),
	})

	protected readonly sortDirection = linkedQueryParam<SortDirection>('sortDirection', {
		parse: value => (value === 'desc' ? 'desc' : 'asc'),
	})

	protected readonly listing = injectQuery(() =>
		this.#options.getList({
			search: this.search(),
			offset: this.offset(),
			limit: this.limit(),
			sortBy: this.sortBy(),
			sortDirection: this.sortDirection(),
		}),
	)

	protected readonly view = signal<'grid' | 'list'>('grid')
	protected readonly breadcrumbs = [{ label: 'Games' }]
	protected readonly pageSizes = PAGE_SIZES
	protected readonly sortOptions = GAME_SORT_OPTIONS
	protected readonly sortLabel = (value: unknown): string => getGameSortLabel(value)

	protected readonly skeletons = computed(() => Array.from({ length: this.limit() }, (value, index) => index))
	protected readonly sortSelection = computed(() => `${this.sortBy()}:${this.sortDirection()}`)
	protected readonly total = computed(() => this.listing.data()?.total ?? 0)

	protected changeSort(value: unknown): void {
		const parsed = parseGameSort(value)

		if (!parsed) return

		this.sortBy.set(parsed.sortBy)
		this.sortDirection.set(parsed.sortDirection)
		this.offset.set(0)
	}

	constructor() {
		effect(() => {
			const page = this.listing.data()

			if (!page || this.listing.isPlaceholderData() || this.listing.isFetching() || !this.listing.isSuccess())
				return

			if (page.offset > 0 && page.offset >= page.total) {
				const offset = page.total ? Math.floor((page.total - 1) / page.limit) * page.limit : 0

				void this.#router.navigate([], {
					queryParams: { offset: offset || null },
					queryParamsHandling: 'merge',
					replaceUrl: true,
				})
			}
		})
	}
}
