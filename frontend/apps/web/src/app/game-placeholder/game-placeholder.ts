import { Component, input } from '@angular/core'
import { RouterLink } from '@angular/router'
import { Button } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matArrowBackRound, matMapRound } from '@ng-icons/material-symbols/round'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'

@Component({
	selector: 'app-game-placeholder',
	imports: [RouterLink, Button, NgIcon, HlmEmptyImports],
	template: `
		<div hlmEmpty class="max-w-lg border">
			<div hlmEmptyHeader>
				<div hlmEmptyMedia variant="icon" class="size-16">
					<ng-icon name="matMapRound" class="text-[--spacing(10)]" aria-hidden="true" />
				</div>
				<h1 hlmEmptyTitle>{{ slug() }}</h1>
				<p hlmEmptyDescription>Interactive maps for this game will appear here soon.</p>
			</div>
			<div hlmEmptyContent>
				<a ui-button variant="outline" routerLink="/" icon="matArrowBackRound">Back to games</a>
			</div>
		</div>
	`,
	providers: [provideIcons({ matArrowBackRound, matMapRound })],
	host: {
		class: 'mx-auto flex w-full max-w-7xl flex-1 items-center justify-center px-4 py-12 sm:px-6',
	},
})
export class GamePlaceholder {
	readonly slug = input.required<string>()
}
