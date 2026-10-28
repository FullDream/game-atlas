import { Portal } from '@angular/cdk/portal'
import { Service, computed, signal } from '@angular/core'
import type { WorkspaceBreadcrumb } from '../navigation/workspace-navigation'

type HeaderContent = { portal: Portal<unknown>; breadcrumbs: WorkspaceBreadcrumb[]; loading: boolean }

@Service()
export class PageHeader {
	readonly #content = signal<HeaderContent | null>(null)
	readonly portal = computed(() => this.#content()?.portal ?? null)
	readonly breadcrumbs = computed(() => this.#content()?.breadcrumbs ?? [])
	readonly loading = computed(() => this.#content()?.loading ?? false)

	attach(portal: Portal<unknown>, breadcrumbs: WorkspaceBreadcrumb[], loading = false): void {
		this.#content.set({ portal, breadcrumbs, loading })
	}

	detach(portal: Portal<unknown>): void {
		this.#content.update(current => (current?.portal === portal ? null : current))
	}
}
