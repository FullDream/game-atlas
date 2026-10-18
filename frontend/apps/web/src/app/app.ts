import { Component } from '@angular/core'
import { RouterOutlet } from '@angular/router'

@Component({
	imports: [RouterOutlet],
	selector: 'app-root',
	templateUrl: './app.html',
	styleUrl: './app.css',
	host: {
		class: 'block',
		role: 'document',
	},
})
export class App {}
