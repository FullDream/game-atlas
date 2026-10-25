import { inject, Injectable } from '@angular/core'
import { Title } from '@angular/platform-browser'
import { RouterStateSnapshot, TitleStrategy } from '@angular/router'

const applicationTitle = 'GameAtlas Admin'

@Injectable()
export class AdminTitleStrategy extends TitleStrategy {
	readonly #title = inject(Title)

	override updateTitle(snapshot: RouterStateSnapshot): void {
		const pageTitle = this.buildTitle(snapshot)

		this.#title.setTitle(pageTitle ? `${pageTitle} | ${applicationTitle}` : applicationTitle)
	}
}
