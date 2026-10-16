import { httpResource } from '@angular/common/http'
import { Injectable } from '@angular/core'
import { parseGames } from './game'

@Injectable({ providedIn: 'root' })
export class GamesCatalog {
	readonly games = httpResource(() => '/content/games.json', { parse: parseGames })
}
