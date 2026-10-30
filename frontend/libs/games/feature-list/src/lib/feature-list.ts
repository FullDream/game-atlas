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
import { GamesQueryOptions, type GameSortBy, type SortDirection } from '@game-atlas/games/data-access'
import { Button, ErrorState, PageHeaderPortal } from '@game-atlas/shared/ui'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmNumberedPaginationQueryParams } from '@spartan-ng/helm/pagination'
import { injectQuery } from '@tanstack/angular-query-experimental'
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
			const number = Number(value ?? 24)

			return [24, 48, 72, 96].includes(number) ? number : 24
		},
		stringify: value => (value === 24 ? null : value),
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

	protected readonly sortLabel = (value: unknown): string =>
		({
			'name:asc': 'Name A–Z',
			'name:desc': 'Name Z–A',
			'createdAt:desc': 'Newest first',
			'createdAt:asc': 'Oldest first',
			'updatedAt:desc': 'Recently updated',
			'updatedAt:asc': 'Least recently updated',
		})[String(value)] ?? 'Sort games'

	protected readonly skeletons = computed(() => Array.from({ length: this.limit() }, (value, index) => index))
	protected readonly sortSelection = computed(() => this.sortBy() + ':' + this.sortDirection())
	protected readonly total = computed(() => this.listing.data()?.total ?? 0)

	protected changeSort(value: unknown): void {
		if (typeof value !== 'string') return

		const [field, direction] = value.split(':')

		if (field !== 'name' && field !== 'createdAt' && field !== 'updatedAt') return

		this.sortBy.set(field)
		this.sortDirection.set(direction === 'desc' ? 'desc' : 'asc')
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
