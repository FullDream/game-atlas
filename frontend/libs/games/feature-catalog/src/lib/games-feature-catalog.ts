import { ChangeDetectionStrategy, Component, inject } from '@angular/core'
import { NgOptimizedImage } from '@angular/common'
import { GamesCatalog } from '@game-atlas/games/data-access'

@Component({
	selector: 'lib-games-feature-catalog',
	imports: [NgOptimizedImage],
	templateUrl: './games-feature-catalog.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameCatalog {
	protected readonly catalog = inject(GamesCatalog)
}
