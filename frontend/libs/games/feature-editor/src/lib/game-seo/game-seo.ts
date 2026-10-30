import { Component, inject, type Signal } from '@angular/core'
import { ROUTER_OUTLET_DATA } from '@angular/router'
import { HlmInput } from '@spartan-ng/helm/input'
import { type FeatureEditorContext } from '../feature-editor-context'

@Component({
	selector: 'games-seo',
	imports: [HlmInput],
	templateUrl: './game-seo.html',
	host: {
		role: 'region',
		'aria-label': 'Search appearance',
		class: 'min-w-0 rounded-xl border-border bg-card p-4 text-card-foreground md:p-6 block border',
	},
})
export class GameSeo {
	protected readonly context = inject(ROUTER_OUTLET_DATA) as Signal<FeatureEditorContext>
}
