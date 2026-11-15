import { booleanAttribute, DestroyRef, Directive, inject, input } from '@angular/core'
import { PageLoading } from './page-loading'

@Directive({ selector: '[uiPageLoading]' })
export class PageLoadingSource {
	readonly loading = input(false, { alias: 'uiPageLoading', transform: booleanAttribute })

	constructor() {
		const unregister = inject(PageLoading).register(this.loading)

		inject(DestroyRef).onDestroy(unregister)
	}
}
