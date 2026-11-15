import { inject, Injectable } from '@angular/core'
import { Title } from '@angular/platform-browser'
import { RouterStateSnapshot, TitleStrategy } from '@angular/router'

@Injectable()
export abstract class PageTitleStrategy extends TitleStrategy {
	readonly #title = inject(Title)

	protected abstract readonly applicationTitle: string

	override updateTitle(snapshot: RouterStateSnapshot): void {
		this.setPageTitle(this.buildTitle(snapshot))
	}

	setPageTitle(pageTitle?: string): void {
		this.#title.setTitle(pageTitle ? `${pageTitle} | ${this.applicationTitle}` : this.applicationTitle)
	}
}
