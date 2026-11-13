import { Component, signal } from '@angular/core'
import { injectExposedSideProvider, injectExposesStateProvider } from '@spartan-ng/brain/core'
import { BrnDrawerHandle } from '@spartan-ng/brain/drawer'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-drawer-content',
	exportAs: 'hlmDrawerContent',
	hostDirectives: [{ directive: BrnDrawerHandle, inputs: ['closeThreshold'] }],
	host: {
		'data-slot': 'drawer-content',
		'[attr.data-vaul-drawer-direction]': 'sideProvider.side()',
		'[attr.data-state]': 'state()',
	},
	template: `
		<div class="bg-muted mt-4 h-1.5 w-24 mx-auto shrink-0 rounded-full" aria-hidden="true"></div>
		<ng-content />
	`,
})
export class HlmDrawerContent {
	protected readonly sideProvider = injectExposedSideProvider({ host: true })

	readonly #stateProvider = injectExposesStateProvider({ host: true })

	readonly state = this.#stateProvider.state ?? signal('closed')

	constructor() {
		classes(() => [
			'before:bg-background before:border-border p-4 text-sm before:inset-2 before:rounded-4xl data-[vaul-drawer-direction=bottom]:inset-x-0 data-[vaul-drawer-direction=bottom]:bottom-0 data-[vaul-drawer-direction=bottom]:mt-24 data-[vaul-drawer-direction=left]:inset-y-0 data-[vaul-drawer-direction=left]:left-0 data-[vaul-drawer-direction=right]:inset-y-0 data-[vaul-drawer-direction=right]:right-0 data-[vaul-drawer-direction=top]:inset-x-0 data-[vaul-drawer-direction=top]:top-0 data-[vaul-drawer-direction=top]:mb-24 data-[vaul-drawer-direction=left]:sm:max-w-sm data-[vaul-drawer-direction=right]:sm:max-w-sm fixed z-50 flex h-auto flex-col bg-transparent before:absolute before:-z-10 before:border data-[vaul-drawer-direction=bottom]:max-h-[80vh] data-[vaul-drawer-direction=left]:w-3/4 data-[vaul-drawer-direction=right]:w-3/4 data-[vaul-drawer-direction=top]:max-h-[80vh]',
			'group/drawer-content',
			'data-open:animate-in data-closed:animate-out',
			'data-[vaul-drawer-direction=bottom]:data-closed:slide-out-to-bottom data-[vaul-drawer-direction=bottom]:data-open:slide-in-from-bottom',
			'data-[vaul-drawer-direction=top]:data-closed:slide-out-to-top data-[vaul-drawer-direction=top]:data-open:slide-in-from-top',
			'data-[vaul-drawer-direction=left]:data-closed:slide-out-to-left data-[vaul-drawer-direction=left]:data-open:slide-in-from-left',
			'data-[vaul-drawer-direction=right]:data-closed:slide-out-to-right data-[vaul-drawer-direction=right]:data-open:slide-in-from-right',
		])
	}
}
