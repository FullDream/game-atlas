import { Component, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matSportsEsportsRound, matPhotoLibraryRound, matMapRound } from '@ng-icons/material-symbols/round'

@Component({
	selector: 'app-foundation-page',
	imports: [NgIcon],
	providers: [provideIcons({ matSportsEsportsRound, matPhotoLibraryRound, matMapRound })],
	host: { class: 'flex flex-1 items-center justify-center p-6 sm:p-10' },
	template: `
		<section class="max-w-lg flex flex-col items-center text-center" aria-labelledby="foundation-title">
			<div
				class="mb-6 size-14 rounded-2xl bg-muted text-muted-foreground flex items-center justify-center"
				aria-hidden="true"
			>
				<ng-icon [name]="icon()" size="26" />
			</div>
			<h2 id="foundation-title" class="text-2xl font-semibold tracking-tight sm:text-3xl">{{ headline() }}</h2>
			<p class="mt-3 max-w-sm text-sm leading-6 text-muted-foreground">{{ description() }}</p>
			@if (note()) {
				<p class="mt-6 text-xs text-muted-foreground">{{ note() }}</p>
			}
		</section>
	`,
})
export class FoundationPage {
	readonly headline = input.required<string>()
	readonly description = input.required<string>()
	readonly note = input('')
	readonly icon = input('matMapRound')
}
