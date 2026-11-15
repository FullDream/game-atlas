import { Component, computed, effect, inject, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { PublicGamesQueryOptions } from '@game-atlas/games/data-access'
import {
	Button,
	ErrorState,
	ImagePreview,
	MediaThumbnail,
	PageLoadingSource,
	PageTitleStrategy,
} from '@game-atlas/shared/ui'
import { formatCount } from '@game-atlas/shared/util'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matArrowBackRound,
	matArrowForwardRound,
	matCategoryRound,
	matCloudOffRound,
	matHomeRound,
	matLocationOnRound,
	matMapRound,
	matRefreshRound,
} from '@ng-icons/material-symbols/round'
import { HlmBreadcrumbImports } from '@spartan-ng/helm/breadcrumb'
import { HlmCardImports } from '@spartan-ng/helm/card'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { injectQuery } from '@tanstack/angular-query-experimental'

@Component({
	selector: 'games-detail',
	imports: [
		RouterLink,
		Button,
		ErrorState,
		ImagePreview,
		MediaThumbnail,
		PageLoadingSource,
		NgIcon,
		HlmBreadcrumbImports,
		HlmCardImports,
		HlmEmptyImports,
		HlmSkeleton,
	],
	templateUrl: './games-feature-detail.html',
	providers: [
		provideIcons({
			matArrowBackRound,
			matArrowForwardRound,
			matCategoryRound,
			matCloudOffRound,
			matHomeRound,
			matLocationOnRound,
			matMapRound,
			matRefreshRound,
		}),
	],
	host: { class: 'mx-auto flex w-full max-w-7xl flex-1 flex-col gap-8 px-4 py-6 sm:gap-10 sm:px-6 sm:py-8' },
})
export class GamesFeatureDetail {
	readonly slug = input.required<string>()

	readonly #options = inject(PublicGamesQueryOptions)
	readonly #title = inject(PageTitleStrategy)

	protected readonly detail = injectQuery(() => this.#options.getDetail(this.slug()))

	protected readonly countLabel = formatCount
	protected readonly game = computed(() => this.detail.data())
	protected readonly partial = computed(() => this.detail.isLoading() || this.detail.isPlaceholderData())

	protected readonly mapSkeletons = computed(() =>
		Array.from({ length: this.game()?.mapsCount ?? 2 }, (value, index) => index),
	)

	constructor() {
		effect(() => this.#title.setPageTitle(this.game()?.name ?? 'Game'))
	}
}
