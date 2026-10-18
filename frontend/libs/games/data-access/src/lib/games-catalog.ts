import { httpResource } from '@angular/common/http'
import { Service } from '@angular/core'
import { parseGames } from './game'

@Service()
export class GamesCatalog {
	readonly games = httpResource(() => '/content/games.json', { parse: parseGames })
}
