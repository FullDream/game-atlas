import { computed, Service, signal, type Signal } from '@angular/core'

@Service()
export class PageLoading {
	readonly #sources = signal<readonly Signal<boolean>[]>([])
	readonly loading = computed(() => this.#sources().some(source => source()))

	register(source: Signal<boolean>): () => void {
		this.#sources.update(sources => [...sources, source])

		return () => this.#sources.update(sources => sources.filter(current => current !== source))
	}
}
