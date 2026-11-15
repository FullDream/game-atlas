import { CdkPortal } from '@angular/cdk/portal'
import { DestroyRef, Directive, effect, inject, input, untracked } from '@angular/core'
import type { WorkspaceBreadcrumb } from '../navigation/workspace-navigation'
import { PageHeader } from './page-header'
import { PageLoading } from '../page-loading/page-loading'

@Directive({ selector: 'ng-template[uiPageHeaderPortal]', hostDirectives: [CdkPortal] })
export class PageHeaderPortal {
	readonly breadcrumbs = input<WorkspaceBreadcrumb[]>([])
	readonly loading = input(false)
	readonly inheritContext = input(false)

	readonly #header = inject(PageHeader, { optional: true })
	readonly #portal = inject(CdkPortal, { self: true })

	constructor() {
		const unregister = inject(PageLoading, { optional: true })?.register(this.loading)

		inject(DestroyRef).onDestroy(() => unregister?.())

		effect(() => {
			const breadcrumbs = this.breadcrumbs()
			const loading = this.loading()
			const inherit = this.inheritContext()

			untracked(() => this.#header?.attach(this.#portal, inherit ? undefined : breadcrumbs, loading))
		})

		inject(DestroyRef).onDestroy(() => this.#header?.detach(this.#portal))
	}
}
