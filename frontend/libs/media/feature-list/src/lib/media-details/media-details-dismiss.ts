import { DialogRef } from '@angular/cdk/dialog'
import { Directive, inject, output } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { filter, merge, tap } from 'rxjs'

@Directive({ selector: '[mediaDetailsDismiss]' })
export class MediaDetailsDismiss {
	readonly dismissRequested = output<void>()

	constructor() {
		const overlay = inject(DialogRef).overlayRef

		merge(
			overlay.backdropClick(),
			overlay.keydownEvents().pipe(
				filter(event => event.key === 'Escape'),
				tap(event => event.preventDefault()),
			),
		)
			.pipe(takeUntilDestroyed())
			.subscribe(() => this.dismissRequested.emit())
	}
}
