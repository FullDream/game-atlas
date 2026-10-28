import { Component, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound, matPhotoLibraryRound, matSportsEsportsRound } from '@ng-icons/material-symbols/round'

@Component({
	selector: 'app-foundation-page',
	imports: [NgIcon],
	templateUrl: './foundation-page.html',
	providers: [provideIcons({ matSportsEsportsRound, matPhotoLibraryRound, matMapRound })],
	host: { class: 'flex flex-1 items-center justify-center p-6 sm:p-10' },
})
export class FoundationPage {
	readonly headline = input.required<string>()
	readonly description = input.required<string>()
	readonly note = input('')
	readonly icon = input('matMapRound')
}
