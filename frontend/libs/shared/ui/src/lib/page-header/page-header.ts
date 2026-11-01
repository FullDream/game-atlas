import { Portal } from '@angular/cdk/portal'
import { Service, computed, signal } from '@angular/core'
import type { WorkspaceBreadcrumb } from '../navigation/workspace-navigation'

type HeaderContent = { portal: Portal<unknown>; breadcrumbs: WorkspaceBreadcrumb[]; loading: boolean }
type HeaderActions = { portal: Portal<unknown>; loading: boolean }

@Service()
export class PageHeader {
	readonly #content = signal<HeaderContent | null>(null)
	readonly #actions = signal<HeaderActions | null>(null)
	readonly portal = computed(() => this.#actions()?.portal ?? this.#content()?.portal ?? null)
	readonly breadcrumbs = computed(() => this.#content()?.breadcrumbs ?? [])
	readonly loading = computed(() => !!(this.#content()?.loading || this.#actions()?.loading))

	attach(portal: Portal<unknown>, breadcrumbs: WorkspaceBreadcrumb[] | undefined, loading = false): void {
		if (breadcrumbs === undefined) this.#actions.set({ portal, loading })
		else this.#content.set({ portal, breadcrumbs, loading })
	}

	detach(portal: Portal<unknown>): void {
		if (this.#actions()?.portal === portal) this.#actions.set(null)

		if (this.#content()?.portal === portal) this.#content.set(null)
	}
}
