import { Component, inject } from '@angular/core'
import { Button } from '../button/button'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'

export type Confirmation = {
	title: string
	description: string
	confirm: string
	cancel: string
	destructive?: boolean
}

@Component({
	selector: 'ui-confirmation-dialog',
	imports: [Button],
	templateUrl: './confirmation-dialog.html',
	host: {
		'[attr.data-state]': 'dialog.state()',
		class: 'block w-[min(28rem,calc(100vw-2rem))] rounded-4xl bg-popover p-6 text-popover-foreground ring-1 ring-foreground/5',
	},
})
export class ConfirmationDialog {
	protected readonly dialog = inject<BrnDialogRef<boolean>>(BrnDialogRef)
	protected readonly context = injectBrnDialogContext<Confirmation>()
}
