import { Injectable } from '@angular/core'
import { PageTitleStrategy } from '@game-atlas/shared/ui'

@Injectable()
export class AdminTitleStrategy extends PageTitleStrategy {
	protected readonly applicationTitle = 'GameAtlas Admin'
}
