import { CdkPortal } from '@angular/cdk/portal'
import { DestroyRef, Directive, effect, inject, input, untracked } from '@angular/core'
import type { WorkspaceBreadcrumb } from '../navigation/workspace-navigation'
import { PageHeader } from './page-header'

@Directive({ selector: 'ng-template[uiPageHeaderPortal]', hostDirectives: [CdkPortal] })
export class PageHeaderPortal {
	readonly breadcrumbs = input<WorkspaceBreadcrumb[]>([])
	readonly loading = input(false)

	readonly #header = inject(PageHeader, { optional: true })
	readonly #portal = inject(CdkPortal, { self: true })

	constructor() {
		effect(() => {
			const breadcrumbs = this.breadcrumbs()
			const loading = this.loading()

			untracked(() => this.#header?.attach(this.#portal, breadcrumbs, loading))
		})

		inject(DestroyRef).onDestroy(() => this.#header?.detach(this.#portal))
	}
}
