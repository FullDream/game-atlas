import { computed, inject, Service } from '@angular/core'
import { Router } from '@angular/router'

@Service()
export class NavigationLoading {
	readonly #router = inject(Router)

	readonly loading = computed(() => this.#router.currentNavigation() !== null)
}
