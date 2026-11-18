import { isPlatformBrowser, NgOptimizedImage } from '@angular/common'
import { Component, computed, effect, inject, linkedSignal, PLATFORM_ID } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { FormsModule } from '@angular/forms'
import { Router } from '@angular/router'
import {
	GAME_SORT_OPTIONS,
	PublicGamesQueryOptions,
	getGameSortLabel,
	type ListPublicGamesSortBy as GameSortBy,
	type ListPublicGamesSortDirection as SortDirection,
	parseGameSort,
} from '@game-atlas/games/data-access'
import { Button, ErrorState, PageLoadingSource } from '@game-atlas/shared/ui'
import { DEFAULT_PAGE_SIZE, PAGE_SIZES } from '@game-atlas/shared/util'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matCloseRound,
	matCloudOffRound,
	matLayersRound,
	matLocationOnRound,
	matRefreshRound,
	matSearchRound,
	matSortRound,
	matSportsEsportsRound,
} from '@ng-icons/material-symbols/round'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { HlmNumberedPaginationQueryParams } from '@spartan-ng/helm/pagination'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { injectQuery } from "@tanstack/angular-query"
import { linkedQueryParam } from 'ngxtension/linked-query-param'
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs'
import { GameCard } from './game-card/game-card'
import { HERO_ATLAS_IMAGE_SRC, HERO_ATLAS_PLACEHOLDER } from './hero-atlas'

@Component({
	selector: 'games-catalog',
	imports: [
		PageLoadingSource,
		NgOptimizedImage,
		FormsModule,
		NgIcon,
		Button,
		ErrorState,
		GameCard,
		HlmEmptyImports,
		HlmInputGroupImports,
		HlmNumberedPaginationQueryParams,
		HlmSelectImports,
	],
	templateUrl: './feature-catalog.html',
	providers: [
		provideIcons({
			matCloseRound,
			matCloudOffRound,
			matLayersRound,
			matLocationOnRound,
			matRefreshRound,
			matSearchRound,
			matSortRound,
			matSportsEsportsRound,
		}),
	],
	host: {
		class: 'flex flex-1 flex-col',
	},
})
export class FeatureCatalog {
	readonly #options = inject(PublicGamesQueryOptions)
	readonly #router = inject(Router)
	readonly #platformId = inject(PLATFORM_ID)

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

	protected readonly heroImage = HERO_ATLAS_IMAGE_SRC
	protected readonly heroPlaceholder = HERO_ATLAS_PLACEHOLDER
	protected readonly pageSizes = PAGE_SIZES
	protected readonly sortOptions = GAME_SORT_OPTIONS

	protected readonly features = [
		{ icon: 'matLocationOnRound', label: 'Unlimited markers' },
		{ icon: 'matLayersRound', label: 'Multiple maps' },
	] as const

	protected readonly sortLabel = (value: unknown): string => getGameSortLabel(value)

	readonly #searchInput$ = new Subject<string>()

	protected readonly searchInput = linkedSignal(() => this.search())
	protected readonly skeletons = computed(() => Array.from({ length: this.limit() }, (value, index) => index))
	protected readonly sortSelection = computed(() => `${this.sortBy()}:${this.sortDirection()}`)
	protected readonly total = computed(() => this.listing.data()?.total ?? 0)

	constructor() {
		this.#searchInput$.pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed()).subscribe(value => {
			if (value === this.search()) return

			this.search.set(value)
			this.offset.set(0)
		})

		effect(() => {
			const page = this.listing.data()

			if (
				!isPlatformBrowser(this.#platformId) ||
				!page ||
				this.listing.isPlaceholderData() ||
				this.listing.isFetching() ||
				!this.listing.isSuccess()
			)
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

	protected onSearchInput(value: string): void {
		this.searchInput.set(value)
		this.#searchInput$.next(value)
	}

	protected clearSearch(): void {
		this.searchInput.set('')
		this.#searchInput$.next('')

		if (this.search() !== '') {
			this.search.set('')
			this.offset.set(0)
		}
	}

	protected changeSort(value: unknown): void {
		const parsed = parseGameSort(value)

		if (!parsed) return

		this.sortBy.set(parsed.sortBy)
		this.sortDirection.set(parsed.sortDirection)
		this.offset.set(0)
	}
}
