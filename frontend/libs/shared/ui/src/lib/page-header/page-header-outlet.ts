import { Directive, inject } from '@angular/core'
import { PageHeader } from './page-header'

@Directive({ selector: '[uiPageHeaderOutlet]', exportAs: 'uiPageHeaderOutlet', providers: [PageHeader] })
export class PageHeaderOutlet {
	readonly #header = inject(PageHeader)
	readonly portal = this.#header.portal
	readonly breadcrumbs = this.#header.breadcrumbs
	readonly loading = this.#header.loading
}
