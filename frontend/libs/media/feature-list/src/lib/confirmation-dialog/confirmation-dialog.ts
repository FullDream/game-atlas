import { Component, inject } from '@angular/core'
import { Button } from '@game-atlas/shared/ui'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'

export type Confirmation = {
	title: string
	description: string
	confirm: string
	cancel: string
	destructive?: boolean
}

@Component({
	selector: 'media-confirmation-dialog',
	imports: [Button],
	templateUrl: './confirmation-dialog.html',
	host: {
		'[attr.data-state]': 'dialog.state()',
		class: 'block w-[min(28rem,calc(100vw-2rem))] rounded-xl border bg-background p-6 text-foreground shadow-xl',
	},
})
export class ConfirmationDialog {
	protected readonly dialog = inject<BrnDialogRef<boolean>>(BrnDialogRef)
	protected readonly context = injectBrnDialogContext<Confirmation>()
}
