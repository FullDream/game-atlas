import { Component, inject, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matDockToLeftRound } from '@ng-icons/material-symbols/round'
import { HlmButton, provideBrnButtonConfig } from '@spartan-ng/helm/button'
import { HlmSidebarService } from './hlm-sidebar.service'

@Component({
	// eslint-disable-next-line @angular-eslint/component-selector
	selector: 'button[hlmSidebarTrigger]',
	exportAs: 'hlmSidebarTrigger',
	imports: [NgIcon],
	providers: [provideIcons({ matDockToLeftRound }), provideBrnButtonConfig({ variant: 'ghost', size: 'icon-sm' })],
	hostDirectives: [{ directive: HlmButton, inputs: ['variant', 'size'] }],
	host: {
		'data-slot': 'sidebar-trigger',
		'data-sidebar': 'trigger',
		'(click)': 'onClick()',
	},
	template: `
		<ng-icon name="matDockToLeftRound" />
		<span class="sr-only">{{ srOnlyText() }}</span>
	`,
})
export class HlmSidebarTrigger {
	readonly #sidebarService = inject(HlmSidebarService)

	public readonly srOnlyText = input<string>('Toggle Sidebar')

	protected onClick(): void {
		this.#sidebarService.toggleSidebar()
	}
}
