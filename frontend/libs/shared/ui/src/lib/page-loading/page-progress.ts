import { Component, computed, inject } from '@angular/core'
import { Router } from '@angular/router'
import { HlmProgress, HlmProgressIndicator } from '@spartan-ng/helm/progress'
import { PageLoading } from './page-loading'

@Component({
	selector: 'ui-page-progress',
	imports: [HlmProgress, HlmProgressIndicator],
	templateUrl: './page-progress.html',
	host: { class: 'pointer-events-none absolute inset-x-0 bottom-0 flex' },
})
export class PageProgress {
	readonly #router = inject(Router)
	readonly #pageLoading = inject(PageLoading)

	protected readonly loading = computed(
		() => this.#router.currentNavigation() !== null || this.#pageLoading.loading(),
	)
}
