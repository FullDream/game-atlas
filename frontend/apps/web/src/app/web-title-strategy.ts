import { Injectable } from '@angular/core'
import { PageTitleStrategy } from '@game-atlas/shared/ui'

@Injectable()
export class WebTitleStrategy extends PageTitleStrategy {
	protected readonly applicationTitle = 'GameAtlas'
}
